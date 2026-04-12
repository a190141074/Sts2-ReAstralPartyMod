using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(ColorlessCardPool))]
public class SkillTroubleMaker : AstralPartyCardModel
{
    private int _cardsToShow = 3;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("StarLight", 3)];

    public SkillTroubleMaker() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        _cardsToShow += 1;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var offeredCards = AstralEventCardPool.CreateRandomMutableEventCardsForPlayer(Owner, _cardsToShow);

        if (offeredCards.Count == 0) return;

        var selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, offeredCards, Owner, false);

        if (selectedCard == null) return;

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature,
            DynamicVars["StarLight"].BaseValue, Owner.Creature, this, false);

        var cardToPlay = CombatState.CreateCard(selectedCard.CanonicalInstance, Owner);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
    }
}
