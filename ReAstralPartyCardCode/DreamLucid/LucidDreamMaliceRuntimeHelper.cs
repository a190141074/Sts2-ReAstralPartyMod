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
    private sealed record DreamModeVisitRecord(int Col, int Row, int Count);
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
    private static readonly HashSet<(int FromCol, int FromRow, int ToCol, int ToRow)> DreamModeDirectedEdges = [];
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
        if (modifier?.EnableDreamMode == true && modifier.IsInDreamModeRevisitedRestSite)
            return 0m;

        if (modifier?.EnableSevereWoundOneMalice != true)
            return amount;

        return Math.Max(0m, amount * 0.5m);
    }

    public static void InitializeDreamModeState(LucidDreamMaliceModifier modifier, RunState? runState)
    {
        if (!modifier.EnableDreamMode || runState == null)
            return;

        if (modifier.DreamPathCols.Count == 0 || modifier.DreamPathRows.Count == 0)
        {
            modifier.DreamPathCols = runState.VisitedMapCoords.Select(static coord => coord.col).ToList();
            modifier.DreamPathRows = runState.VisitedMapCoords.Select(static coord => coord.row).ToList();
        }

        if (modifier.DreamVisitCols.Count == 0 || modifier.DreamVisitRows.Count == 0 || modifier.DreamVisitCounts.Count == 0)
        {
            var visits = runState.VisitedMapCoords
                .GroupBy(static coord => (coord.col, coord.row))
                .Select(static group => new DreamModeVisitRecord(group.Key.col, group.Key.row, group.Count()))
                .OrderBy(static entry => entry.Row)
                .ThenBy(static entry => entry.Col)
                .ToList();
            modifier.DreamVisitCols = visits.Select(static entry => entry.Col).ToList();
            modifier.DreamVisitRows = visits.Select(static entry => entry.Row).ToList();
            modifier.DreamVisitCounts = visits.Select(static entry => entry.Count).ToList();
        }
    }

    public static bool IsDreamModeEnabled(IRunState? runState)
    {
        return ReAstralPartyModSettingsManager.GetEnableDreamMode(runState);
    }

    public static bool IsDreamModeEnabled(Creature? creature)
    {
        return IsDreamModeEnabled(creature?.CombatState?.RunState);
    }

    public static bool ApplyDreamModeToMap(RunState? runState, ActMap? map)
    {
        if (runState == null || map == null)
            return false;

        var modifier = GetModifier(runState);
        if (modifier?.EnableDreamMode != true)
            return false;

        var points = map.GetAllMapPoints().ToList();
        if (points.Count == 0)
            return false;

        DreamModeDirectedEdges.Clear();

        var originalEdges = new List<(MapPoint From, MapPoint To)>();
        foreach (var point in points)
        {
            foreach (var child in point.Children.ToList())
                originalEdges.Add((point, child));
        }

        foreach (var point in points)
        {
            foreach (var child in point.Children.ToList())
                point.RemoveChildPoint(child);
        }

        var allByCoord = points.ToDictionary(static point => (point.coord.col, point.coord.row));
        var rewired = new HashSet<(MapPoint From, MapPoint To)>();

        void AddEdge(MapPoint from, MapPoint to)
        {
            if (ReferenceEquals(from, to))
                return;
            if (!rewired.Add((from, to)))
                return;

            from.AddChildPoint(to);
            DreamModeDirectedEdges.Add((from.coord.col, from.coord.row, to.coord.col, to.coord.row));
        }

        foreach (var point in points)
        {
            var candidates = points
                .Where(other => !ReferenceEquals(other, point) && IsDreamModeLinkCandidate(point, other))
                .OrderBy(other => GetDreamModeDistanceSquared(point, other))
                .ThenBy(other => other.coord.row)
                .ThenBy(other => other.coord.col)
                .Take(3)
                .ToList();

            foreach (var candidate in PruneDreamModeCollinearEdges(point, candidates))
            {
                AddEdge(point, candidate);
                AddEdge(candidate, point);
            }
        }

        foreach (var (from, to) in originalEdges)
        {
            if (HasDreamModeUndirectedNeighbor(from, to))
                continue;

            AddEdge(from, to);
            AddEdge(to, from);
        }

        if (map.StartingMapPoint != null)
        {
            foreach (var point in points.Where(point => point.coord.row == 0 && !ReferenceEquals(point, map.StartingMapPoint)))
            {
                AddEdge(map.StartingMapPoint, point);
                AddEdge(point, map.StartingMapPoint);
            }
        }

        if (map.BossMapPoint != null)
        {
            foreach (var point in points.Where(point => point.coord.row == map.GetRowCount() - 1 && !ReferenceEquals(point, map.BossMapPoint)))
            {
                AddEdge(point, map.BossMapPoint);
                AddEdge(map.BossMapPoint, point);
            }
        }

        RefreshMapScreenPointsIfNeeded(runState, map);
        return true;
    }

    public static IEnumerable<MapPoint> GetDreamModeNeighbors(RunState? runState, MapPoint point)
    {
        if (runState == null || !IsDreamModeEnabled(runState))
            return point.Children;

        return point.parents.Concat(point.Children).Distinct();
    }

    public static bool HasVisitedCoord(LucidDreamMaliceModifier? modifier, MapCoord coord)
    {
        if (modifier == null)
            return false;

        for (var i = 0; i < modifier.DreamVisitCols.Count && i < modifier.DreamVisitRows.Count && i < modifier.DreamVisitCounts.Count; i++)
        {
            if (modifier.DreamVisitCols[i] == coord.col && modifier.DreamVisitRows[i] == coord.row && modifier.DreamVisitCounts[i] > 0)
                return true;
        }

        return false;
    }

    public static int GetVisitCount(LucidDreamMaliceModifier? modifier, MapCoord coord)
    {
        if (modifier == null)
            return 0;

        for (var i = 0; i < modifier.DreamVisitCols.Count && i < modifier.DreamVisitRows.Count && i < modifier.DreamVisitCounts.Count; i++)
        {
            if (modifier.DreamVisitCols[i] == coord.col && modifier.DreamVisitRows[i] == coord.row)
                return modifier.DreamVisitCounts[i];
        }

        return 0;
    }

    public static void RegisterDreamVisit(LucidDreamMaliceModifier modifier, MapCoord coord)
    {
        modifier.DreamPathCols.Add(coord.col);
        modifier.DreamPathRows.Add(coord.row);

        for (var i = 0; i < modifier.DreamVisitCols.Count && i < modifier.DreamVisitRows.Count && i < modifier.DreamVisitCounts.Count; i++)
        {
            if (modifier.DreamVisitCols[i] == coord.col && modifier.DreamVisitRows[i] == coord.row)
            {
                modifier.DreamVisitCounts[i]++;
                return;
            }
        }

        modifier.DreamVisitCols.Add(coord.col);
        modifier.DreamVisitRows.Add(coord.row);
        modifier.DreamVisitCounts.Add(1);
    }

    public static void SetDreamModeRevisitedRestSite(LucidDreamMaliceModifier? modifier, bool value)
    {
        if (modifier == null)
            return;

        modifier.IsInDreamModeRevisitedRestSite = value;
    }

    public static bool ShouldSkipRoomContentOnDreamModeRevisit(MapPoint? point)
    {
        return point != null && point.PointType is not (MapPointType.Shop or MapPointType.RestSite or MapPointType.Boss);
    }

    public static bool TryResolveTravelPathTicks(
        IDictionary<(MapCoord, MapCoord), IReadOnlyList<Godot.TextureRect>> paths,
        MapCoord from,
        MapCoord to,
        out IReadOnlyList<Godot.TextureRect>? ticks)
    {
        if (paths.TryGetValue((from, to), out ticks))
            return true;

        return paths.TryGetValue((to, from), out ticks);
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

    private static bool IsDreamModeLinkCandidate(MapPoint from, MapPoint to)
    {
        var rowDelta = Math.Abs(from.coord.row - to.coord.row);
        var colDelta = Math.Abs(from.coord.col - to.coord.col);
        if (rowDelta == 0)
            return colDelta <= 2;

        return rowDelta <= 2 && colDelta <= 2;
    }

    private static int GetDreamModeDistanceSquared(MapPoint from, MapPoint to)
    {
        var dx = from.coord.col - to.coord.col;
        var dy = from.coord.row - to.coord.row;
        return (dx * dx) + (dy * dy);
    }

    private static IEnumerable<MapPoint> PruneDreamModeCollinearEdges(MapPoint origin, List<MapPoint> candidates)
    {
        if (candidates.Count <= 1)
            return candidates;

        var selected = new List<MapPoint>();
        foreach (var candidate in candidates)
        {
            var shouldKeep = true;
            for (var i = selected.Count - 1; i >= 0; i--)
            {
                var existing = selected[i];
                if (GetDreamModeEdgeAngleDegrees(origin, existing, candidate) >= 20f)
                    continue;

                if (GetDreamModeDistanceSquared(origin, candidate) < GetDreamModeDistanceSquared(origin, existing))
                    selected.RemoveAt(i);
                else
                    shouldKeep = false;
            }

            if (shouldKeep)
                selected.Add(candidate);
        }

        return selected;
    }

    private static float GetDreamModeEdgeAngleDegrees(MapPoint origin, MapPoint first, MapPoint second)
    {
        var firstX = first.coord.col - origin.coord.col;
        var firstY = first.coord.row - origin.coord.row;
        var secondX = second.coord.col - origin.coord.col;
        var secondY = second.coord.row - origin.coord.row;

        var firstLength = Math.Sqrt((firstX * firstX) + (firstY * firstY));
        var secondLength = Math.Sqrt((secondX * secondX) + (secondY * secondY));
        if (firstLength <= double.Epsilon || secondLength <= double.Epsilon)
            return 180f;

        var dot = (firstX * secondX) + (firstY * secondY);
        var cos = Math.Clamp(dot / (firstLength * secondLength), -1d, 1d);
        return (float)(Math.Acos(cos) * 180d / Math.PI);
    }

    private static bool HasDreamModeUndirectedNeighbor(MapPoint first, MapPoint second)
    {
        return DreamModeDirectedEdges.Contains((first.coord.col, first.coord.row, second.coord.col, second.coord.row))
               || DreamModeDirectedEdges.Contains((second.coord.col, second.coord.row, first.coord.col, first.coord.row));
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
