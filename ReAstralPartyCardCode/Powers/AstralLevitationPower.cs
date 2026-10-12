using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class AstralLevitationPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner != null)
            await AstralTemporaryDexterityPower.Apply(Owner, 2m, this, applier, cardSource, true);
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (Owner?.Player == card.Owner && card.Type == CardType.Curse && card.Pile?.Type == PileType.Hand)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this, false);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (Owner?.Player == card.Owner && card.Type == CardType.Curse && card.Pile?.Type != PileType.Draw)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this, false);
    }

    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner || dealer == null || dealer.Side == Owner?.Side || amount <= 0m)
            return 1m;
        if (!props.HasFlag(ValueProp.Move))
            return 1m;
        return 0.5m;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner != null && side == Owner.Side)
            return MegaCrit.Sts2.Core.Commands.PowerCmd.TickDownDuration(this);
        return Task.CompletedTask;
    }
}
