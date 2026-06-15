using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

[RegisterGoodModifier]
public sealed class LucidDreamMaliceModifier : ModifierModel
{
    private const int MaxSavedDreamStateEntries = 512;

    private List<int> _dreamPathCols = [];
    private List<int> _dreamPathRows = [];
    private List<int> _dreamVisitCols = [];
    private List<int> _dreamVisitRows = [];
    private List<int> _dreamVisitCounts = [];

    // Keep public runtime names stable while SavedProperty only sees prefixed backing fields.
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableFalseLifeline { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableSmoothSailing { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableDreamMode { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableFishScalesMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableSevereWoundOneMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableSevereWoundTwoMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableMadLifeMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableSwampOfFateMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableOverpopulationMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableCautiousJellyfishMalice { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableFaceDeathWithComposure { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableWildness { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableWildnessPhantom { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnablePitchBlackImpulse { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableBubblePotionOfDreams { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceEnableHarmlessWhisper { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceHasSpawnedOverpopulationEnemyThisRun { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMalicePendingOverpopulationSpawnThisCombat { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceIsInDreamModeEmptyRevisitCombatRoom { get; set; }
    [SavedProperty] private bool AstralParty_LucidDreamMaliceHasDreamModePendingRevisit { get; set; }
    [SavedProperty] private int AstralParty_LucidDreamMaliceDreamModePendingRevisitCol { get; set; }
    [SavedProperty] private int AstralParty_LucidDreamMaliceDreamModePendingRevisitRow { get; set; }
    [SavedProperty] private int AstralParty_LucidDreamMaliceDreamModePendingRevisitPointTypeRaw { get; set; }

    public bool EnableFalseLifeline
    {
        get => AstralParty_LucidDreamMaliceEnableFalseLifeline;
        set => AstralParty_LucidDreamMaliceEnableFalseLifeline = value;
    }

    public bool EnableSmoothSailing
    {
        get => AstralParty_LucidDreamMaliceEnableSmoothSailing;
        set => AstralParty_LucidDreamMaliceEnableSmoothSailing = value;
    }

    public bool EnableDreamMode
    {
        get => AstralParty_LucidDreamMaliceEnableDreamMode;
        set => AstralParty_LucidDreamMaliceEnableDreamMode = value;
    }

    public bool EnableFishScalesMalice
    {
        get => AstralParty_LucidDreamMaliceEnableFishScalesMalice;
        set => AstralParty_LucidDreamMaliceEnableFishScalesMalice = value;
    }

    public bool EnableSevereWoundOneMalice
    {
        get => AstralParty_LucidDreamMaliceEnableSevereWoundOneMalice;
        set => AstralParty_LucidDreamMaliceEnableSevereWoundOneMalice = value;
    }

    public bool EnableSevereWoundTwoMalice
    {
        get => AstralParty_LucidDreamMaliceEnableSevereWoundTwoMalice;
        set => AstralParty_LucidDreamMaliceEnableSevereWoundTwoMalice = value;
    }

    public bool EnableMadLifeMalice
    {
        get => AstralParty_LucidDreamMaliceEnableMadLifeMalice;
        set => AstralParty_LucidDreamMaliceEnableMadLifeMalice = value;
    }

    public bool EnableSwampOfFateMalice
    {
        get => AstralParty_LucidDreamMaliceEnableSwampOfFateMalice;
        set => AstralParty_LucidDreamMaliceEnableSwampOfFateMalice = value;
    }

    public bool EnableOverpopulationMalice
    {
        get => AstralParty_LucidDreamMaliceEnableOverpopulationMalice;
        set => AstralParty_LucidDreamMaliceEnableOverpopulationMalice = value;
    }

    public bool EnableCautiousJellyfishMalice
    {
        get => AstralParty_LucidDreamMaliceEnableCautiousJellyfishMalice;
        set => AstralParty_LucidDreamMaliceEnableCautiousJellyfishMalice = value;
    }

    public bool EnableFaceDeathWithComposure
    {
        get => AstralParty_LucidDreamMaliceEnableFaceDeathWithComposure;
        set => AstralParty_LucidDreamMaliceEnableFaceDeathWithComposure = value;
    }

    public bool EnableWildness
    {
        get => AstralParty_LucidDreamMaliceEnableWildness;
        set => AstralParty_LucidDreamMaliceEnableWildness = value;
    }

    public bool EnableWildnessPhantom
    {
        get => AstralParty_LucidDreamMaliceEnableWildnessPhantom;
        set => AstralParty_LucidDreamMaliceEnableWildnessPhantom = value;
    }

    public bool EnablePitchBlackImpulse
    {
        get => AstralParty_LucidDreamMaliceEnablePitchBlackImpulse;
        set => AstralParty_LucidDreamMaliceEnablePitchBlackImpulse = value;
    }

    public bool EnableBubblePotionOfDreams
    {
        get => AstralParty_LucidDreamMaliceEnableBubblePotionOfDreams;
        set => AstralParty_LucidDreamMaliceEnableBubblePotionOfDreams = value;
    }

    public bool EnableHarmlessWhisper
    {
        get => AstralParty_LucidDreamMaliceEnableHarmlessWhisper;
        set => AstralParty_LucidDreamMaliceEnableHarmlessWhisper = value;
    }

    public bool HasSpawnedOverpopulationEnemyThisRun
    {
        get => AstralParty_LucidDreamMaliceHasSpawnedOverpopulationEnemyThisRun;
        set => AstralParty_LucidDreamMaliceHasSpawnedOverpopulationEnemyThisRun = value;
    }

    public bool PendingOverpopulationSpawnThisCombat
    {
        get => AstralParty_LucidDreamMalicePendingOverpopulationSpawnThisCombat;
        set => AstralParty_LucidDreamMalicePendingOverpopulationSpawnThisCombat = value;
    }

    public bool IsInDreamModeEmptyRevisitCombatRoom
    {
        get => AstralParty_LucidDreamMaliceIsInDreamModeEmptyRevisitCombatRoom;
        set => AstralParty_LucidDreamMaliceIsInDreamModeEmptyRevisitCombatRoom = value;
    }

    public bool HasDreamModePendingRevisit
    {
        get => AstralParty_LucidDreamMaliceHasDreamModePendingRevisit;
        set => AstralParty_LucidDreamMaliceHasDreamModePendingRevisit = value;
    }

    public int DreamModePendingRevisitCol
    {
        get => AstralParty_LucidDreamMaliceDreamModePendingRevisitCol;
        set => AstralParty_LucidDreamMaliceDreamModePendingRevisitCol = value;
    }

    public int DreamModePendingRevisitRow
    {
        get => AstralParty_LucidDreamMaliceDreamModePendingRevisitRow;
        set => AstralParty_LucidDreamMaliceDreamModePendingRevisitRow = value;
    }

    public int DreamModePendingRevisitPointTypeRaw
    {
        get => AstralParty_LucidDreamMaliceDreamModePendingRevisitPointTypeRaw;
        set => AstralParty_LucidDreamMaliceDreamModePendingRevisitPointTypeRaw = value;
    }

    // BaseLib [SavedProperty] does not support List<T>; keep lists runtime-only and save stable JSON strings.
    [SavedProperty]
    private string AstralParty_LucidDreamMaliceDreamPathColsJson
    {
        get => SerializeIntList(_dreamPathCols);
        set => _dreamPathCols = DeserializeIntList(value, nameof(AstralParty_LucidDreamMaliceDreamPathColsJson));
    }

    [SavedProperty]
    private string AstralParty_LucidDreamMaliceDreamPathRowsJson
    {
        get => SerializeIntList(_dreamPathRows);
        set => _dreamPathRows = DeserializeIntList(value, nameof(AstralParty_LucidDreamMaliceDreamPathRowsJson));
    }

    [SavedProperty]
    private string AstralParty_LucidDreamMaliceDreamVisitColsJson
    {
        get => SerializeIntList(_dreamVisitCols);
        set => _dreamVisitCols = DeserializeIntList(value, nameof(AstralParty_LucidDreamMaliceDreamVisitColsJson));
    }

    [SavedProperty]
    private string AstralParty_LucidDreamMaliceDreamVisitRowsJson
    {
        get => SerializeIntList(_dreamVisitRows);
        set => _dreamVisitRows = DeserializeIntList(value, nameof(AstralParty_LucidDreamMaliceDreamVisitRowsJson));
    }

    [SavedProperty]
    private string AstralParty_LucidDreamMaliceDreamVisitCountsJson
    {
        get => SerializeIntList(_dreamVisitCounts);
        set => _dreamVisitCounts = DeserializeIntList(value, nameof(AstralParty_LucidDreamMaliceDreamVisitCountsJson));
    }

    public List<int> DreamPathCols
    {
        get => _dreamPathCols;
        set => _dreamPathCols = value ?? [];
    }

    public List<int> DreamPathRows
    {
        get => _dreamPathRows;
        set => _dreamPathRows = value ?? [];
    }

    public List<int> DreamVisitCols
    {
        get => _dreamVisitCols;
        set => _dreamVisitCols = value ?? [];
    }

    public List<int> DreamVisitRows
    {
        get => _dreamVisitRows;
        set => _dreamVisitRows = value ?? [];
    }

    public List<int> DreamVisitCounts
    {
        get => _dreamVisitCounts;
        set => _dreamVisitCounts = value ?? [];
    }

    [SavedProperty] private bool AstralParty_LucidDreamMaliceIsInDreamModeRevisitedRestSite { get; set; }

    public bool IsInDreamModeRevisitedRestSite
    {
        get => AstralParty_LucidDreamMaliceIsInDreamModeRevisitedRestSite;
        set => AstralParty_LucidDreamMaliceIsInDreamModeRevisitedRestSite = value;
    }

    public override bool ShouldReceiveCombatHooks => true;

    public bool HasAnyEnabled =>
        EnableFalseLifeline
        || EnableSmoothSailing
        || EnableDreamMode
        ||
        EnableFishScalesMalice
        || EnableSevereWoundOneMalice
        || EnableSevereWoundTwoMalice
        || EnableMadLifeMalice
        || EnableSwampOfFateMalice
        || EnableOverpopulationMalice
        || EnableCautiousJellyfishMalice
        || EnableFaceDeathWithComposure
        || EnableWildness
        || EnableWildnessPhantom
        || EnablePitchBlackImpulse
        || EnableBubblePotionOfDreams
        || EnableHarmlessWhisper;

    public void ApplySnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        EnableFalseLifeline = snapshot.EnableLucidDreamFalseLifeline;
        EnableSmoothSailing = snapshot.EnableLucidDreamSmoothSailing;
        EnableDreamMode = snapshot.EnableDreamMode;
        EnableFishScalesMalice = snapshot.EnableLucidDreamFishScalesMalice;
        EnableSevereWoundOneMalice = snapshot.EnableLucidDreamSevereWoundOneMalice;
        EnableSevereWoundTwoMalice = snapshot.EnableLucidDreamSevereWoundTwoMalice;
        EnableMadLifeMalice = snapshot.EnableLucidDreamMadLifeMalice;
        EnableSwampOfFateMalice = snapshot.EnableLucidDreamSwampOfFateMalice;
        EnableOverpopulationMalice = snapshot.EnableLucidDreamOverpopulationMalice;
        EnableCautiousJellyfishMalice = snapshot.EnableLucidDreamCautiousJellyfishMalice;
        EnableFaceDeathWithComposure = snapshot.EnableLucidDreamFaceDeathWithComposure;
        EnableWildness = snapshot.EnableLucidDreamWildness;
        EnableWildnessPhantom = snapshot.EnableLucidDreamWildnessPhantom;
        EnablePitchBlackImpulse = snapshot.EnableLucidDreamPitchBlackImpulse;
        EnableBubblePotionOfDreams = snapshot.EnableLucidDreamBubblePotionOfDreams;
        EnableHarmlessWhisper = snapshot.EnableLucidDreamHarmlessWhisper;
    }

    public static LucidDreamMaliceModifier? Get(RunState? runState)
    {
        return runState?.Modifiers.OfType<LucidDreamMaliceModifier>().FirstOrDefault();
    }

    private static string SerializeIntList(IReadOnlyList<int> values)
    {
        return JsonSerializer.Serialize(values.Take(MaxSavedDreamStateEntries).ToArray());
    }

    private static List<int> DeserializeIntList(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return (JsonSerializer.Deserialize<int[]>(value) ?? [])
                .Take(MaxSavedDreamStateEntries)
                .ToList();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[LucidDreamMalice] Failed to deserialize saved dream state '{propertyName}'; resetting that cache. {ex.GetType().Name}: {ex.Message}");
            return [];
        }
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        return LucidDreamMaliceRuntimeHelper.TryIncreaseIncomingDebuffStack(
            this,
            canonicalPower,
            target,
            amount,
            applier,
            out modifiedAmount);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        return LucidDreamMaliceRuntimeHelper.TryApplyEnergyCostAdjustment(
            this,
            card,
            originalCost,
            out modifiedCost);
    }

    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        return LucidDreamMaliceRuntimeHelper.TryApplyStarCostAdjustment(
            this,
            card,
            originalCost,
            out modifiedCost);
    }

    public override int ModifyXValue(CardModel card, int originalValue)
    {
        return LucidDreamMaliceRuntimeHelper.ModifyXValue(this, card, originalValue);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return LucidDreamMaliceRuntimeHelper.ModifyMaxEnergy(this, amount);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var additive = LucidDreamMaliceRuntimeHelper.GetFaceDeathWithComposureDamageBonus(
            this,
            target,
            amount,
            props,
            dealer,
            cardSource);
        if (additive != 0m)
            return additive;

        return LucidDreamMaliceRuntimeHelper.GetWildnessDamageBonus(
            this,
            target,
            amount,
            props,
            dealer,
            cardSource);
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        var multiplier = LucidDreamMaliceRuntimeHelper.GetWildnessBlockMultiplier(this, target, block);
        if (multiplier != 1m)
            return multiplier;
        if (target.Player == null || !EnableSevereWoundTwoMalice || block <= 0m)
            return 1m;

        return 0.5m;
    }

    public override Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (amount > 0m)
            LucidDreamMaliceRuntimeHelper.TryClampCurrentBlockToMaxHp(creature);

        return Task.CompletedTask;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (!EnableBubblePotionOfDreams || creature.Player == null)
            return amount;

        MainFile.Logger.Info(
            $"[DreamLucid] Bubble Potion suppressing rest heal via modifier: finalHeal={amount}.");
        return LucidDreamMaliceRuntimeHelper.CaptureBubblePotionOfDreamsSuppressedHeal(creature.Player, amount);
    }

    public override Task AfterRestSiteHeal(Player player, bool isMimicked)
    {
        if (!EnableBubblePotionOfDreams)
            return Task.CompletedTask;

        MainFile.Logger.Info(
            $"[DreamLucid] Bubble Potion finalizing rest heal via modifier: goldPreview={LucidDreamMaliceRuntimeHelper.PeekBubblePotionOfDreamsGoldPreview(player)}.");
        return LucidDreamMaliceRuntimeHelper.FinalizeBubblePotionOfDreamsRestHealAsync(player);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        return LucidDreamMaliceRuntimeHelper.HandlePitchBlackImpulseAfterDamageGiven(
            this,
            choiceContext,
            dealer,
            result,
            props,
            target,
            cardSource);
    }

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        PendingOverpopulationSpawnThisCombat =
            EnableOverpopulationMalice
            && !HasSpawnedOverpopulationEnemyThisRun
            && room is CombatRoom
            && room.RoomType is not (RoomType.Elite or RoomType.Boss);
        return Task.CompletedTask;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            IsInDreamModeEmptyRevisitCombatRoom = false;

        if (!EnableFalseLifeline || RunState == null)
            return;

        foreach (var player in RunState.Players.OrderBy(static player => player.NetId))
        {
            var healAmount = LucidDreamMaliceRuntimeHelper.CalculateFalseLifelineHealAmount(player, room.RoomType);
            if (healAmount <= 0m || player.Creature == null)
                continue;

            await CreatureCmd.Heal(player.Creature, healAmount, true);
        }
    }

    public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
    {
        var changed = false;
        if (EnableSmoothSailing && LucidDreamMaliceRuntimeHelper.ApplySmoothSailingToMap(map))
            changed = true;
        if (EnableDreamMode && LucidDreamMaliceRuntimeHelper.ApplyDreamModeToMap(RunState, map))
            changed = true;

        if (changed)
            LucidDreamMaliceRuntimeHelper.RefreshMapScreenPointsIfNeeded(RunState, map);

        return map;
    }

    public override async Task BeforeCombatStart()
    {
        if (EnableWildness)
        {
            var combatRoomForWildness = RunState?.CurrentRoom as CombatRoom;
            await LucidDreamMaliceRuntimeHelper.ApplyWildnessToCombatAsync(
                RunState,
                combatRoomForWildness?.CombatState);
        }

        if (!PendingOverpopulationSpawnThisCombat)
            return;

        PendingOverpopulationSpawnThisCombat = false;

        var combatRoom = RunState?.CurrentRoom as CombatRoom;
        var combatState = combatRoom?.CombatState;
        var encounter = combatState?.Encounter;
        if (combatState == null || encounter == null)
            return;
        if (encounter.RoomType is RoomType.Elite or RoomType.Boss)
            return;

        var selectedMonster = LucidDreamMaliceRuntimeHelper.PickOverpopulationMonster(RunState, encounter);
        if (selectedMonster == null)
        {
            MainFile.Logger.Warn("[LucidDreamMalice] Overpopulation skipped because no weak-encounter monster candidate was found.");
            return;
        }

        var slot = LucidDreamMaliceRuntimeHelper.ResolveOverpopulationSlot(combatState);
        var creature = await CreatureCmd.Add(selectedMonster.ToMutable(), combatState, CombatSide.Enemy, slot);
        LucidDreamMaliceRuntimeHelper.MarkOverpopulationSpawn(creature);
        if (EnableWildness)
            await LucidDreamMaliceRuntimeHelper.ApplyWildnessToCreatureAsync(RunState, creature);

        HasSpawnedOverpopulationEnemyThisRun = true;
        MainFile.Logger.Info(
            $"[LucidDreamMalice] Overpopulation added extra enemy {selectedMonster.Id.Entry} into active combat for encounter {encounter.Id.Entry} | slot={(slot ?? "<auto-layout>")}.");
    }

}
