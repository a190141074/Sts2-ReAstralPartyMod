using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
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
    [SavedProperty]
    public bool EnableFalseLifeline { get; set; }

    [SavedProperty]
    public bool EnableSmoothSailing { get; set; }

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
    public bool EnableFaceDeathWithComposure { get; set; }

    [SavedProperty]
    public bool EnableWildness { get; set; }

    [SavedProperty]
    public bool EnablePitchBlackImpulse { get; set; }

    [SavedProperty]
    public bool EnableBubblePotionOfDreams { get; set; }

    [SavedProperty]
    public bool EnableHarmlessWhisper { get; set; }

    [SavedProperty]
    public bool HasSpawnedOverpopulationEnemyThisRun { get; set; }

    [SavedProperty]
    public bool PendingOverpopulationSpawnThisCombat { get; set; }

    public override bool ShouldReceiveCombatHooks => true;

    public bool HasAnyEnabled =>
        EnableFalseLifeline
        || EnableSmoothSailing
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
        || EnablePitchBlackImpulse
        || EnableBubblePotionOfDreams
        || EnableHarmlessWhisper;

    public void ApplySnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        EnableFalseLifeline = snapshot.EnableLucidDreamFalseLifeline;
        EnableSmoothSailing = snapshot.EnableLucidDreamSmoothSailing;
        EnableFishScalesMalice = snapshot.EnableLucidDreamFishScalesMalice;
        EnableSevereWoundOneMalice = snapshot.EnableLucidDreamSevereWoundOneMalice;
        EnableSevereWoundTwoMalice = snapshot.EnableLucidDreamSevereWoundTwoMalice;
        EnableMadLifeMalice = snapshot.EnableLucidDreamMadLifeMalice;
        EnableSwampOfFateMalice = snapshot.EnableLucidDreamSwampOfFateMalice;
        EnableOverpopulationMalice = snapshot.EnableLucidDreamOverpopulationMalice;
        EnableCautiousJellyfishMalice = snapshot.EnableLucidDreamCautiousJellyfishMalice;
        EnableFaceDeathWithComposure = snapshot.EnableLucidDreamFaceDeathWithComposure;
        EnableWildness = snapshot.EnableLucidDreamWildness;
        EnablePitchBlackImpulse = snapshot.EnableLucidDreamPitchBlackImpulse;
        EnableBubblePotionOfDreams = snapshot.EnableLucidDreamBubblePotionOfDreams;
        EnableHarmlessWhisper = snapshot.EnableLucidDreamHarmlessWhisper;
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
        if (!EnableSmoothSailing)
            return map;

        if (LucidDreamMaliceRuntimeHelper.ApplySmoothSailingToMap(map))
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

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        return false;
    }
}
