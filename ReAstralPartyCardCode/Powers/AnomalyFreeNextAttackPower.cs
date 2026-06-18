using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class AnomalyFreeNextAttackPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override LocString Title => new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ANOMALY_FREE_NEXT_ATTACK_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ANOMALY_FREE_NEXT_ATTACK_POWER.description");

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (Owner?.Player == null)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(card) || card.EnergyCost.CostsX)
            return false;

        modifiedCost = 0m;
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardPlay.Card))
            return;

        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner?.Player?.PlayerCombatState == null)
            return Task.CompletedTask;

        foreach (var card in oldOwner.Player.PlayerCombatState.Hand.Cards)
            card.InvokeEnergyCostChanged();

        return Task.CompletedTask;
    }
}
