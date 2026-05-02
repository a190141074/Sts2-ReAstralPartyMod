using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventFightFun : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventFightFun() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        var giantRockCard = ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.GiantRock>();

        foreach (var player in CombatState.Players)
        {
            var card = CombatState.CreateCard(giantRockCard, player);
            CardCmd.Upgrade(card);
            card.AddKeyword(CardKeyword.Exhaust);
            card.AddKeyword(CardKeyword.Ethereal);
            await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
            await XiaoLeiAwakeningHelper.TryGrantAwakeningForGrantedCard(Owner, player);
        }
    }
}