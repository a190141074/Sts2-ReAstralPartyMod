using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

/*
 * 天降神兵
 * 从所有的事件中(除了机械降神)选择一张事件卡，打出两次
 */
[Pool(typeof(ColorlessCardPool))]
public class EventDeusExMachina : AstralPartyCardModel
{
    private static readonly LocString SelectionPrompt = new("cards", "EVENT_DEUS_EX_MACHINA.select_prompt");

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

        var offeredCards = AstralEventCardPool.CreateMutableEventCardsForPlayer(Owner, typeof(EventDeusExMachina));

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
