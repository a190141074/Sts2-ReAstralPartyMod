using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Hooks;
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
    [SavedProperty]
    public bool EnableFishScalesMalice { get; set; }

    [SavedProperty]
    public bool EnableSevereWoundOneMalice { get; set; }

    [SavedProperty]
    public bool EnableSevereWoundTwoMalice { get; set; }

    [SavedProperty]
    public bool EnableMadLifeMalice { get; set; }

    [SavedProperty]
    public bool EnableSwampOfFateMalice { get; set; }

    [SavedProperty]
    public bool EnableOverpopulationMalice { get; set; }

    [SavedProperty]
    public bool EnableCautiousJellyfishMalice { get; set; }

    [SavedProperty]
    public bool HasSpawnedOverpopulationEnemyThisRun { get; set; }

    [SavedProperty]
    public bool PendingOverpopulationSpawnThisCombat { get; set; }

    public override bool ShouldReceiveCombatHooks => true;

    public bool HasAnyEnabled =>
        EnableFishScalesMalice
        || EnableSevereWoundOneMalice
        || EnableSevereWoundTwoMalice
        || EnableMadLifeMalice
        || EnableSwampOfFateMalice
        || EnableOverpopulationMalice
        || EnableCautiousJellyfishMalice;

    public void ApplySnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        EnableFishScalesMalice = snapshot.EnableLucidDreamFishScalesMalice;
        EnableSevereWoundOneMalice = snapshot.EnableLucidDreamSevereWoundOneMalice;
        EnableSevereWoundTwoMalice = snapshot.EnableLucidDreamSevereWoundTwoMalice;
        EnableMadLifeMalice = snapshot.EnableLucidDreamMadLifeMalice;
        EnableSwampOfFateMalice = snapshot.EnableLucidDreamSwampOfFateMalice;
        EnableOverpopulationMalice = snapshot.EnableLucidDreamOverpopulationMalice;
        EnableCautiousJellyfishMalice = snapshot.EnableLucidDreamCautiousJellyfishMalice;
    }

    public static LucidDreamMaliceModifier? Get(RunState? runState)
    {
        return runState?.Modifiers.OfType<LucidDreamMaliceModifier>().FirstOrDefault();
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

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
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

    public override Task BeforeRoomEntered(AbstractRoom room)
    {
        PendingOverpopulationSpawnThisCombat =
            EnableOverpopulationMalice
            && !HasSpawnedOverpopulationEnemyThisRun
            && room is CombatRoom
            && room.RoomType is not (RoomType.Elite or RoomType.Boss);
        return Task.CompletedTask;
    }

    public override async Task BeforeCombatStart()
    {
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

        HasSpawnedOverpopulationEnemyThisRun = true;
        MainFile.Logger.Info(
            $"[LucidDreamMalice] Overpopulation added extra enemy {selectedMonster.Id.Entry} into active combat for encounter {encounter.Id.Entry} | slot={(slot ?? "<auto-layout>")}.");
    }
}
