using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

/*
 * 天降神兵
 * 从所有的事件�?除了机械降神)选择一张事件卡，打出两�? */
[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 2)]
public class EventDeusExMachina : AstralPartyCardModel
{
    private static readonly LocString SelectionPrompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_EVENT_DEUS_EX_MACHINA.select_prompt");

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventDeusExMachina() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var offeredCards = AstralEventCardCatalog.CreateEventCards(typeof(EventDeusExMachina))
            .Select(card =>
            {
                var displayCard = (card.CanonicalInstance ?? card).ToMutable();
                displayCard.Owner = Owner;
                return displayCard;
            })
            .ToList();

        if (offeredCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionPrompt, 1)
        {
            Cancelable = false,
            PretendCardsCanBePlayed = true
        };

        var selectedCard = (await CardSelectCmd.FromSimpleGrid(choiceContext, offeredCards, Owner, prefs))
            .FirstOrDefault();
        if (selectedCard == null) return;

        for (var i = 0; i < 2; i++)
        {
            var cardToPlay = CombatState.CreateCard(selectedCard.CanonicalInstance, Owner);
            await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
        }
    }
}
