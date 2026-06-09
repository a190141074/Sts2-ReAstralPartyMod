using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using HarmonyLib;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

internal static class LucidDreamMaliceRuntimeHelper
{
    private sealed record WeakEncounterMonsterCandidate(MonsterModel Monster, string StableId);
    private sealed class PitchBlackImpulseSequenceState
    {
        public HashSet<ulong> HitTargetIds { get; } = [];
        public int TotalHitCount { get; set; }
        public int DistinctTargetCount { get; set; }
        public List<decimal> PendingSplashDamages { get; } = [];
    }

    private static readonly HashSet<uint> OverpopulationSpawnCombatIds = [];
    private static readonly ConditionalWeakTable<IRunState, Dictionary<string, PitchBlackImpulseSequenceState>>
        PitchBlackImpulseSequences = new();
    private static readonly ConcurrentDictionary<Player, decimal> BubblePotionSuppressedHealByPlayer = new();
    private static readonly AccessTools.FieldRef<NMapScreen, RunState?> MapScreenRunStateField =
        AccessTools.FieldRefAccess<NMapScreen, RunState?>("_runState");
    private static int _pitchBlackImpulseSyntheticDepth;

    public static LucidDreamMaliceModifier? GetModifier(IRunState? runState)
    {
        return LucidDreamMaliceModifier.Get(runState as RunState);
    }

    public static LucidDreamMaliceModifier? GetModifier(Creature? creature)
    {
        return GetModifier(creature?.CombatState?.RunState);
    }

    public static decimal AdjustHealAmount(Creature? target, decimal amount)
    {
        if (target?.Player == null || amount <= 0m)
            return amount;

        var modifier = GetModifier(target);
        if (modifier?.EnableSevereWoundOneMalice != true)
            return amount;

        return Math.Max(0m, amount * 0.5m);
    }

    public static decimal AdjustBlockGainAmount(Creature? target, decimal amount)
    {
        if (target?.Player == null || amount <= 0m)
            return amount;

        var modifier = GetModifier(target);
        if (modifier?.EnableSevereWoundTwoMalice != true)
            return amount;

        return Math.Max(0m, amount * 0.5m);
    }

    public static bool TryClampCurrentBlockToMaxHp(Creature? creature)
    {
        if (creature?.Player == null)
            return false;

        var modifier = GetModifier(creature);
        if (modifier?.EnableFishScalesMalice != true)
            return false;

        var maxBlock = Math.Max(0m, Convert.ToDecimal(creature.MaxHp));
        if (creature.Block <= maxBlock)
            return false;

        creature.LoseBlockInternal(creature.Block - maxBlock);
        return true;
    }

    public static bool TryIncreaseIncomingDebuffStack(
        LucidDreamMaliceModifier modifier,
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (!modifier.EnableMadLifeMalice || amount <= 0m)
            return false;
        if (target.Player == null || target.Side != CombatSide.Player)
            return false;
        if (applier?.Side != CombatSide.Enemy)
            return false;
        if (!StackableDebuffGrowthHelper.CanIncreaseIncomingStackableDebuff(canonicalPower, amount))
            return false;

        modifiedAmount += 1m;
        return true;
    }

    public static bool TryApplyEnergyCostAdjustment(
        LucidDreamMaliceModifier modifier,
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (!modifier.EnableCautiousJellyfishMalice)
            return false;
        if (card.EnergyCost.CostsX || originalCost < 0m)
            return false;

        if (originalCost >= 4m)
            return false;

        var adjustedCost = originalCost + 1m;
        if (adjustedCost == originalCost)
            return false;

        modifiedCost = adjustedCost;
        return modifiedCost != originalCost;
    }

    public static bool TryApplyStarCostAdjustment(
        LucidDreamMaliceModifier modifier,
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (!modifier.EnableCautiousJellyfishMalice)
            return false;
        if (card.HasStarCostX || originalCost < 0m)
            return false;

        if (originalCost >= 4m)
            return false;

        var adjustedCost = originalCost + 1m;
        if (adjustedCost == originalCost)
            return false;

        modifiedCost = adjustedCost;
        return modifiedCost != originalCost;
    }

    public static int ModifyXValue(LucidDreamMaliceModifier modifier, CardModel card, int originalValue)
    {
        if (!modifier.EnableCautiousJellyfishMalice)
            return originalValue;
        if (!card.EnergyCost.CostsX && !card.HasStarCostX)
            return originalValue;

        return Math.Max(0, originalValue - 2);
    }

    public static decimal ModifyMaxEnergy(LucidDreamMaliceModifier modifier, decimal originalAmount)
    {
        if (!modifier.EnableCautiousJellyfishMalice)
            return originalAmount;

        return originalAmount + 4m;
    }

    public static async Task HandlePitchBlackImpulseAfterDamageGiven(
        LucidDreamMaliceModifier modifier,
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!modifier.EnablePitchBlackImpulse)
            return;
        if (dealer == null || result.TotalDamage <= 0)
            return;

        await TryGrantPitchBlackImpulseStrengthOnKillAsync(dealer, result);

        if (IsPitchBlackImpulseSyntheticDamage())
            return;

        await TryApplyPitchBlackImpulseSplashToOtherPlayersAsync(
            modifier,
            choiceContext,
            dealer,
            result,
            props,
            target,
            cardSource);
    }

    public static bool TryRedirectPitchBlackImpulseTarget(
        PlayerChoiceContext choiceContext,
        ref Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (amount <= 0m)
            return false;
        if (dealer?.CombatState?.RunState == null)
            return false;
        if (dealer.CombatState == null || target.CombatState != dealer.CombatState)
            return false;
        if (IsPitchBlackImpulseSyntheticDamage())
            return false;

        var modifier = GetModifier(dealer.CombatState.RunState);
        if (modifier?.EnablePitchBlackImpulse != true)
            return false;
        if (!ShouldTreatAsExplicitMultihit(dealer, cardSource))
            return false;
        if (!props.HasFlag(ValueProp.Move))
            return false;

        var runState = dealer.CombatState.RunState;
        var sequenceKey = BuildPitchBlackImpulseSequenceKey(runState, dealer, cardSource);
        var sequence = GetOrCreatePitchBlackImpulseSequence(runState, sequenceKey);
        var hitIndex = sequence.TotalHitCount;

        var randomTarget = PickPitchBlackImpulseRandomTarget(
            dealer,
            runState,
            cardSource,
            hitIndex,
            target.CombatState);
        sequence.TotalHitCount++;

        if (randomTarget == null || randomTarget == target)
            return false;

        target = randomTarget;
        return true;
    }

    public static IDisposable EnterPitchBlackImpulseSyntheticDamageScope()
    {
        _pitchBlackImpulseSyntheticDepth++;
        return new AnonymousDisposable(() => _pitchBlackImpulseSyntheticDepth--);
    }

    public static decimal GetFaceDeathWithComposureDamageBonus(
        LucidDreamMaliceModifier modifier,
        Creature? target,
        decimal amount,
        MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!modifier.EnableFaceDeathWithComposure || amount <= 0m)
            return 0m;
        if (target == null || dealer == null)
            return 0m;
        if (target.Side == dealer.Side)
            return 0m;
        if (!props.HasFlag(MegaCrit.Sts2.Core.ValueProps.ValueProp.Move))
            return 0m;

        return amount;
    }

    public static decimal GetWildnessDamageBonus(
        LucidDreamMaliceModifier modifier,
        Creature? target,
        decimal amount,
        MegaCrit.Sts2.Core.ValueProps.ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!modifier.EnableWildness || amount <= 0m)
            return 0m;
        if (dealer?.Side != CombatSide.Enemy)
            return 0m;
        if (target == null || target.Side == dealer.Side)
            return 0m;
        if (!HasWildnessMarker(dealer))
            return 0m;
        if (!props.HasFlag(MegaCrit.Sts2.Core.ValueProps.ValueProp.Move))
            return 0m;

        var multiplier = GetWildnessMultiplier(dealer.CombatState?.RunState);
        return Math.Max(0m, amount * multiplier);
    }

    public static decimal GetWildnessBlockMultiplier(
        LucidDreamMaliceModifier modifier,
        Creature target,
        decimal block)
    {
        if (!modifier.EnableWildness || block <= 0m)
            return 1m;
        if (target.Side != CombatSide.Enemy)
            return 1m;
        if (!HasWildnessMarker(target))
            return 1m;

        return 1m + GetWildnessMultiplier(target.CombatState?.RunState);
    }

    public static async Task ApplyWildnessToCombatAsync(IRunState? runState, CombatState? combatState)
    {
        if (runState == null || combatState == null)
            return;

        foreach (var enemy in combatState.Enemies.Where(static enemy => enemy != null && enemy.IsAlive))
            await ApplyWildnessToCreatureAsync(runState, enemy);
    }

    public static async Task EnsureWildnessAppliedToActiveCombatAsync(RunState? runState)
    {
        var combatState = runState?.CurrentRoom is CombatRoom combatRoom
            ? combatRoom.CombatState
            : null;
        await ApplyWildnessToCombatAsync(runState, combatState);
    }

    public static async Task ApplyWildnessToCreatureAsync(IRunState? runState, Creature? creature)
    {
        if (runState == null || creature == null || creature.Side != CombatSide.Enemy)
            return;
        if (creature.GetPower<LucidDreamWildnessPower>() != null)
            return;

        var bonusPercent = GetWildnessBonusPercent(runState);
        await PowerCmd.Apply<LucidDreamWildnessPower>(creature, bonusPercent, creature, null, false);

        var maxHpBonus = Math.Ceiling(Math.Max(0m, creature.MaxHp) * GetWildnessMultiplier(runState));
        if (maxHpBonus > 0m)
            await CreatureCmd.GainMaxHp(creature, maxHpBonus);
    }

    public static bool ShouldConvertBubblePotionOfDreamsRestHeal(Player? player)
    {
        if (player?.Creature == null)
            return false;

        return ReAstralPartyModSettingsManager.GetEnableLucidDreamBubblePotionOfDreams(player.RunState);
    }

    public static decimal CaptureBubblePotionOfDreamsSuppressedHeal(Player player, decimal healAmount)
    {
        var normalizedHeal = Math.Max(0m, healAmount);
        BubblePotionSuppressedHealByPlayer[player] = normalizedHeal;
        return 0m;
    }

    public static decimal PeekBubblePotionOfDreamsGoldPreview(Player? player)
    {
        if (player == null)
            return 0m;

        return BubblePotionSuppressedHealByPlayer.TryGetValue(player, out var suppressedHeal)
            ? Math.Max(0m, suppressedHeal * 3m)
            : 0m;
    }

    public static async Task FinalizeBubblePotionOfDreamsRestHealAsync(Player? player)
    {
        if (player == null)
            return;
        if (!BubblePotionSuppressedHealByPlayer.TryRemove(player, out var suppressedHeal))
            return;
        if (suppressedHeal <= 0m)
            return;

        await PersonaMultiplayerEffectHelper.GainGoldDeterministic(suppressedHeal * 3m, player);
    }

    public static int GetWildnessBonusPercent(IRunState? runState)
    {
        return GetActNumber(runState) * 15;
    }

    private static decimal GetWildnessMultiplier(IRunState? runState)
    {
        return GetActNumber(runState) * 0.15m;
    }

    private static int GetActNumber(IRunState? runState)
    {
        return Math.Max((runState?.CurrentActIndex ?? 0) + 1, 1);
    }

    private static bool HasWildnessMarker(Creature creature)
    {
        return creature.GetPower<LucidDreamWildnessPower>() != null;
    }

    public static string? ResolveOverpopulationSlot(CombatState combatState)
    {
        var slot = combatState.Encounter?.GetNextSlot(combatState);
        return string.IsNullOrWhiteSpace(slot) ? null : slot;
    }

    public static void MarkOverpopulationSpawn(Creature creature)
    {
        if (creature.CombatId.HasValue)
            OverpopulationSpawnCombatIds.Add(creature.CombatId.Value);
    }

    public static bool ConsumeOverpopulationSpawnMark(Creature creature)
    {
        return creature.CombatId.HasValue && OverpopulationSpawnCombatIds.Remove(creature.CombatId.Value);
    }

    public static MonsterModel? PickOverpopulationMonster(IRunState? runState, EncounterModel? encounter)
    {
        var resolvedRunState = runState as RunState;
        if (resolvedRunState?.Act == null)
            return null;

        var candidates = BuildWeakEncounterMonsterCandidates(resolvedRunState.Act);
        if (candidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            "lucid_dream_overpopulation",
            resolvedRunState.Rng.StringSeed,
            resolvedRunState.CurrentActIndex,
            resolvedRunState.TotalFloor,
            encounter?.Id.Entry ?? "<none>");
        return candidates[selectedIndex].Monster;
    }

    private static List<WeakEncounterMonsterCandidate> BuildWeakEncounterMonsterCandidates(ActModel act)
    {
        var candidates = new List<WeakEncounterMonsterCandidate>();
        foreach (var encounter in act.AllWeakEncounters)
        {
            var encounterId = (encounter.CanonicalInstance ?? encounter).Id.Entry;
            var occurrence = 0;
            foreach (var monster in encounter.AllPossibleMonsters)
            {
                var canonicalMonster = monster.CanonicalInstance ?? monster;
                candidates.Add(new WeakEncounterMonsterCandidate(
                    canonicalMonster,
                    $"{encounterId}:{canonicalMonster.Id.Entry}:{occurrence++}"));
            }
        }

        candidates.Sort(static (left, right) => string.CompareOrdinal(left.StableId, right.StableId));
        return candidates;
    }

    public static decimal CalculateFalseLifelineHealAmount(Player? player, RoomType roomType)
    {
        var creature = player?.Creature;
        if (creature == null)
            return 0m;

        var missingHp = Math.Max(0m, creature.MaxHp - creature.CurrentHp);
        if (missingHp <= 0m)
            return 0m;

        var multiplier = roomType is RoomType.RestSite or RoomType.Shop ? 0.5m : 0.25m;
        return Math.Max(1m, Math.Ceiling(missingHp * multiplier));
    }

    public static bool ApplySmoothSailingToMap(ActMap? map)
    {
        if (map == null)
            return false;

        var changed = false;
        foreach (var point in map.GetAllMapPoints())
        {
            if (point.PointType != MapPointType.Elite)
                continue;

            point.PointType = MapPointType.Monster;
            changed = true;
        }

        return changed;
    }

    public static void RefreshMapScreenPointsIfNeeded(RunState? runState, ActMap? map)
    {
        if (runState == null || map == null)
            return;

        var screen = NMapScreen.Instance;
        if (screen == null)
            return;

        if (MapScreenRunStateField(screen) != runState)
            return;

        var mapPointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
        if (mapPointDictionaryField?.GetValue(screen) is not IDictionary<MapCoord, NMapPoint> mapPointDictionary)
            return;

        foreach (var point in map.GetAllMapPoints())
            if (mapPointDictionary.TryGetValue(point.coord, out var mapPoint))
                mapPoint.RefreshVisualsInstantly();
    }

    private static bool IsPitchBlackImpulseSyntheticDamage()
    {
        return _pitchBlackImpulseSyntheticDepth > 0;
    }

    private static async Task TryGrantPitchBlackImpulseStrengthOnKillAsync(Creature dealer, DamageResult result)
    {
        if (!result.WasTargetKilled || dealer.CombatState == null || !dealer.IsAlive)
            return;

        await PowerCmd.Apply<StrengthPower>(dealer, 3m, dealer, null, true);
    }

    private static async Task TryApplyPitchBlackImpulseSplashToOtherPlayersAsync(
        LucidDreamMaliceModifier modifier,
        PlayerChoiceContext choiceContext,
        Creature dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!TryCollectPitchBlackImpulseSplashDamages(modifier, dealer, result, target, cardSource, out var splashDamages))
            return;
        var splashTargets = dealer.CombatState!.RunState.Players
            .Where(player => player.Creature != null
                             && player.Creature != dealer
                             && player.Creature != target
                             && player.Creature.CombatState == dealer.CombatState
                             && player.Creature.IsAlive
                             && player.Creature.IsHittable)
            .Select(player => player.Creature!)
            .ToList();
        if (splashTargets.Count == 0)
            return;

        using var _ = EnterPitchBlackImpulseSyntheticDamageScope();
        foreach (var splashDamage in splashDamages)
        {
            foreach (var splashTarget in splashTargets)
                await CreatureCmd.Damage(
                    choiceContext,
                    splashTarget,
                    splashDamage,
                    ValueProp.Unpowered | ValueProp.Move,
                    dealer,
                    cardSource);
        }
    }

    private static bool TryCollectPitchBlackImpulseSplashDamages(
        LucidDreamMaliceModifier modifier,
        Creature dealer,
        DamageResult result,
        Creature target,
        CardModel? cardSource,
        out IReadOnlyList<decimal> splashDamages)
    {
        splashDamages = Array.Empty<decimal>();
        if (dealer.CombatState?.RunState == null)
            return false;
        if (!modifier.EnablePitchBlackImpulse)
            return false;
        if (result.TotalDamage <= 0)
            return false;

        var runState = dealer.CombatState.RunState;
        var sequenceKey = BuildPitchBlackImpulseSequenceKey(runState, dealer, cardSource);
        var sequence = GetOrCreatePitchBlackImpulseSequence(runState, sequenceKey);
        var receiverId = target.CombatId ?? 0u;
        if (receiverId != 0u && sequence.HitTargetIds.Add(receiverId))
            sequence.DistinctTargetCount++;

        sequence.PendingSplashDamages.Add(Math.Max(1m, Math.Ceiling(result.TotalDamage * 0.5m)));
        if (sequence.DistinctTargetCount < 2)
            return false;

        splashDamages = sequence.PendingSplashDamages.ToArray();
        sequence.PendingSplashDamages.Clear();
        return splashDamages.Count > 0;
    }

    private static PitchBlackImpulseSequenceState GetOrCreatePitchBlackImpulseSequence(IRunState runState, string key)
    {
        var table = PitchBlackImpulseSequences.GetOrCreateValue(runState);
        if (!table.TryGetValue(key, out var state))
        {
            state = new PitchBlackImpulseSequenceState();
            table[key] = state;
        }

        return state;
    }

    private static Creature? PickPitchBlackImpulseRandomTarget(
        Creature dealer,
        IRunState runState,
        CardModel? cardSource,
        int hitIndex,
        CombatState? combatState)
    {
        if (combatState == null)
            return null;

        var candidates = combatState.Creatures
            .Where(creature => creature != null
                               && creature != dealer
                               && creature.IsAlive
                               && creature.IsHittable
                               && (creature.Side == CombatSide.Enemy || creature.Player != null))
            .OrderBy(creature => creature.CombatId ?? 0u)
            .ToList();
        if (candidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            "lucid_dream_pitch_black_impulse",
            runState.Rng.StringSeed,
            runState.CurrentActIndex,
            runState.TotalFloor,
            combatState.RoundNumber,
            dealer.CombatId ?? 0u,
            cardSource?.Id.Entry ?? "<no-card>",
            hitIndex);
        return candidates[selectedIndex];
    }

    private static string BuildPitchBlackImpulseSequenceKey(IRunState runState, Creature dealer, CardModel? cardSource)
    {
        var combatState = dealer.CombatState;
        return string.Join(
            "|",
            runState.CurrentActIndex,
            runState.TotalFloor,
            combatState?.RoundNumber ?? 0,
            dealer.CombatId ?? 0u,
            cardSource?.Id.Entry ?? "<no-card>");
    }

    private static bool ShouldTreatAsExplicitMultihit(Creature dealer, CardModel? cardSource)
    {
        if (cardSource != null
            && cardSource.DynamicVars.ContainsKey("Repeat")
            && cardSource.DynamicVars["Repeat"].BaseValue > 1m)
            return true;

        return dealer.Monster?.NextMove?.Intents.OfType<MultiAttackIntent>().Any(intent => intent.Repeats > 1) == true;
    }

    private sealed class AnonymousDisposable(Action dispose) : IDisposable
    {
        private Action? _dispose = dispose;

        public void Dispose()
        {
            var action = _dispose;
            _dispose = null;
            action?.Invoke();
        }
    }
}
