using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(CurseCardPool), StableEntryStem = "enigmatic_the_infinitum")]
public class EnigmaticTheInfinitum : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Eternal];

    public EnigmaticTheInfinitum()
        : base(-2, CardType.Curse, CardRarity.Rare, TargetType.Self, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
        CardCmd.ApplyKeyword(this, CardKeyword.Innate);
    }

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return Task.CompletedTask;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        await base.AfterCardChangedPiles(card, oldPileType, source);

        if (card != this || Pile?.Type != PileType.Exhaust || oldPileType == PileType.Exhaust)
            return;
        if (Owner?.Creature?.CombatState == null)
            return;

        await PowerCmd.Apply(
            ModelDb.Power<EnigmaticInfinitumOmenPower>().ToMutable(),
            Owner.Creature,
            1m,
            Owner.Creature,
            this,
            false);
    }
}
