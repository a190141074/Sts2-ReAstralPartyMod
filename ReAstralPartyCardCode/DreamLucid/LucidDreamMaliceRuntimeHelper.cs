using System;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using HarmonyLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

internal static class LucidDreamMaliceRuntimeHelper
{
    private sealed record WeakEncounterMonsterCandidate(MonsterModel Monster, string StableId);
    private static readonly HashSet<uint> OverpopulationSpawnCombatIds = [];
    private static readonly AccessTools.FieldRef<NMapScreen, RunState?> MapScreenRunStateField =
        AccessTools.FieldRefAccess<NMapScreen, RunState?>("_runState");

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

    public static decimal CalculateFalseLifelineHealAmount(Player? player)
    {
        var creature = player?.Creature;
        if (creature == null)
            return 0m;

        var missingHp = Math.Max(0m, creature.MaxHp - creature.CurrentHp);
        if (missingHp <= 0m)
            return 0m;

        return Math.Max(1m, Math.Ceiling(missingHp * 0.25m));
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
}
