using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class SkillPointPower : AstralPartyPowerModel
{
    private bool _isRepairingUnauthorizedState;

    [SavedProperty] public decimal AstralParty_SkillPointPowerLastAcceptedAmount { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => (int)Amount;

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.description");

    protected override string SmartDescriptionLocKey =>
        "RE_ASTRAL_PARTY_MOD_POWER_SKILL_POINT_POWER.smartDescription";

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not SkillPointPower || target != Owner || amount <= 0m)
            return false;

        if (SkillPointAuthorizationHelper.IsAuthorizedPositiveGrant(target))
            return false;

        modifiedAmount = 0m;
        return true;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (Owner?.Player?.PlayerCombatState == null || Amount <= 0m)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (!WarforgeEnchantmentHelper.CountsAsSkill(card) || card.EnergyCost.CostsX)
            return false;

        var availableEnergy = Owner.Player.PlayerCombatState.Energy;
        if (originalCost <= availableEnergy)
            return false;

        var deficit = originalCost - availableEnergy;
        if (deficit <= 0m || deficit > Amount)
            return false;

        modifiedCost = availableEnergy;
        return modifiedCost != originalCost;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await ReconcileAcceptedAmountAfterChange();
        RefreshHandEnergyCosts(Owner?.Player);
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not SkillPointPower || power != this)
            return;

        await ReconcileAcceptedAmountAfterChange();
        RefreshHandEnergyCosts(Owner?.Player);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player?.PlayerCombatState == null || cardPlay.Card.Owner != Owner.Player)
            return;
        if (!WarforgeEnchantmentHelper.CountsAsSkill(cardPlay.Card) || cardPlay.Card.EnergyCost.CostsX)
            return;

        var totalCost = GetPlayedSkillCost(cardPlay);
        if (totalCost <= 0m)
            return;

        var spentEnergy = cardPlay.Resources.EnergyValue;
        var spentSkillPoints = Math.Max(0m, totalCost - spentEnergy);
        if (spentSkillPoints <= 0m)
            return;

        var nextAmount = Math.Max(0m, Amount - spentSkillPoints);
        AstralParty_SkillPointPowerLastAcceptedAmount = nextAmount;

        if (nextAmount <= 0m)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(this, -spentSkillPoints, Owner, cardPlay.Card, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        AstralParty_SkillPointPowerLastAcceptedAmount = 0m;
        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        AstralParty_SkillPointPowerLastAcceptedAmount = 0m;
        RefreshHandEnergyCosts(oldOwner?.Player);
        return Task.CompletedTask;
    }

    internal static async Task GrantFromRelic(Creature target, decimal amount, RelicModel sourceRelic)
    {
        if (target.Player == null || amount <= 0m)
            return;

        await SkillPointAuthorizationHelper.RunAuthorizedGrant(target, sourceRelic, () =>
            PowerCmd.Apply(
                ModelDb.Power<SkillPointPower>().ToMutable(),
                target,
                amount,
                sourceRelic.Owner?.Creature,
                null,
                false));
    }

    private async Task ReconcileAcceptedAmountAfterChange()
    {
        if (_isRepairingUnauthorizedState || Owner == null)
            return;

        if (Amount <= 0m)
        {
            AstralParty_SkillPointPowerLastAcceptedAmount = 0m;
            return;
        }

        var currentAmount = Amount;
        var acceptedAmount = Math.Max(0m, AstralParty_SkillPointPowerLastAcceptedAmount);
        var isAuthorizedPositiveGrant = SkillPointAuthorizationHelper.IsAuthorizedPositiveGrant(Owner);

        if (currentAmount > acceptedAmount)
        {
            if (isAuthorizedPositiveGrant)
            {
                AstralParty_SkillPointPowerLastAcceptedAmount = currentAmount;
                return;
            }

            _isRepairingUnauthorizedState = true;
            try
            {
                if (acceptedAmount <= 0m)
                    await PowerCmd.Remove(this);
                else
                    await PowerCmd.SetAmount<SkillPointPower>(Owner, acceptedAmount, Owner, null);
            }
            finally
            {
                _isRepairingUnauthorizedState = false;
            }

            return;
        }

        AstralParty_SkillPointPowerLastAcceptedAmount = currentAmount;
    }

    private static decimal GetPlayedSkillCost(CardPlay cardPlay)
    {
        if (cardPlay.Card.EnergyCost.CostsX)
            return Math.Max(1m, cardPlay.Resources.EnergyValue);

        return Math.Max(0m, cardPlay.Card.EnergyCost.GetResolved());
    }

    private static void RefreshHandEnergyCosts(Player? player)
    {
        foreach (var card in player?.PlayerCombatState?.Hand?.Cards ?? [])
            card.InvokeEnergyCostChanged();
    }
}
