using System.Collections.Generic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeGlitchRobot : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeGlitchRobotWingsThisCombat { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeGlitchRobotDamageThresholdThisCombat { get; set; } = 70;
    [SavedProperty] public int AstralParty_PersonalityDerivativeGlitchRobotObservedDivineSonAmount { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeGlitchRobotLastProcessedRound { get; set; }
    [SavedProperty] public bool AstralParty_PersonalityDerivativeGlitchRobotOpeningInitializedThisCombat { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => true;

    public override int DisplayAmount => Math.Max(AstralParty_PersonalityDerivativeGlitchRobotWingsThisCombat, 0);

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<DivineSonPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ResetCombatState();
        SyncDisplay();
    }

    public override async Task BeforeCombatStart()
    {
        await InitializeOpeningStateForCombat();
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;
        if (power.Owner != Owner.Creature)
            return;
        if (power is not DivineSonPower)
            return;

        var currentAmount = AstralNoaHelper.GetRoundedPowerAmount(power.Amount);
        var delta = currentAmount - AstralParty_PersonalityDerivativeGlitchRobotObservedDivineSonAmount;
        AstralParty_PersonalityDerivativeGlitchRobotObservedDivineSonAmount = currentAmount;
        if (delta <= 0)
            return;

        AstralParty_PersonalityDerivativeGlitchRobotWingsThisCombat += delta;
        SyncDisplay();
        await Task.CompletedTask;
    }

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return amount;
        if (amount < AstralParty_PersonalityDerivativeGlitchRobotDamageThresholdThisCombat)
            return amount;

        AstralParty_PersonalityDerivativeGlitchRobotDamageThresholdThisCombat =
            AstralNoaHelper.GetNextThreshold(AstralParty_PersonalityDerivativeGlitchRobotDamageThresholdThisCombat);
        Flash();
        return 0m;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        ResetCombatState();
        SyncDisplay();
        return Task.CompletedTask;
    }

    public int GetCurrentWings()
    {
        return Math.Max(AstralParty_PersonalityDerivativeGlitchRobotWingsThisCombat, 0);
    }

    public void SyncDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    public async Task InitializeOpeningStateForCombat()
    {
        if (AstralParty_PersonalityDerivativeGlitchRobotOpeningInitializedThisCombat)
        {
            SyncDisplay();
            return;
        }

        ResetCombatState();
        AstralParty_PersonalityDerivativeGlitchRobotOpeningInitializedThisCombat = true;
        if (Owner?.Creature == null)
        {
            SyncDisplay();
            return;
        }

        var startingStacks = AstralNoaHelper.GetStartingDivineSonStacksForAct(Owner);
        if (startingStacks > 0)
            await PowerCmd.Apply<DivineSonPower>(Owner.Creature, startingStacks, Owner.Creature, null, false);

        AstralParty_PersonalityDerivativeGlitchRobotObservedDivineSonAmount =
            AstralNoaHelper.GetRoundedPowerAmount(Owner.Creature.GetPowerAmount<DivineSonPower>());
        SyncDisplay();
    }

    private void ResetCombatState()
    {
        AstralParty_PersonalityDerivativeGlitchRobotWingsThisCombat = 0;
        AstralParty_PersonalityDerivativeGlitchRobotDamageThresholdThisCombat = 70;
        AstralParty_PersonalityDerivativeGlitchRobotObservedDivineSonAmount = 0;
        AstralParty_PersonalityDerivativeGlitchRobotLastProcessedRound = 0;
        AstralParty_PersonalityDerivativeGlitchRobotOpeningInitializedThisCombat = false;
    }
}
