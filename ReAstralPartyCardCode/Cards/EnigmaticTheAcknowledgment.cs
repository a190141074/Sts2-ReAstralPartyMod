using MegaCrit.Sts2.Core.Commands;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(CurseCardPool), StableEntryStem = "enigmatic_the_acknowledgment")]
public class EnigmaticTheAcknowledgment : AstralPartyCardModel
{
    protected override string CardId => "enigmatic_the_acknowledgment";

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Eternal];

    public EnigmaticTheAcknowledgment()
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
}
