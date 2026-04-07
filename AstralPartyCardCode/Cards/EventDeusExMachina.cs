using System.Linq;
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

    public override string? CustomPortraitPath => PortraitPath;

    public EventDeusExMachina() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var offeredCards = ModelDb.AllCards
            .Where(card => card is AstralPartyCardModel)
            .Where(card => card.GetType().Name.StartsWith("Event"))
            .Where(card => card.GetType() != typeof(EventDeusExMachina))
            .Where(card => card.GetType() != typeof(SkillTroubleMaker))
            .OrderBy(_ => Owner.RunState.Rng.Niche.NextInt(int.MaxValue))
            .Select(card =>
            {
                var mutableCard = card.ToMutable();
                mutableCard.Owner = Owner;
                return mutableCard;
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