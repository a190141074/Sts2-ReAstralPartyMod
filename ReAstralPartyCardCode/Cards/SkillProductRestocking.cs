using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillProductRestocking : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillProductRestocking() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        var mascotGirlMimi = Owner.GetRelic<PersonMascotGirlMimi>();
        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var discardedCardCount = handCards.Count;

        // This card leaves the hand before its effect resolves, so add it back when calculating
        // how many cards to draw from the player's original hand size.
        var cardsToDraw = discardedCardCount + 1;

        if (discardedCardCount > 0)
            await CardGainAttribution.RunWithSource(this,
                () => CardCmd.DiscardAndDraw(choiceContext, handCards, cardsToDraw));
        else
            await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Draw(choiceContext, cardsToDraw, Owner));

        if (mascotGirlMimi == null)
            return;

        if (discardedCardCount > 0)
        {
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                Owner.Creature,
                discardedCardCount,
                Owner.Creature,
                this,
                false
            );
        }

        await mascotGirlMimi.HandleProductRestockingDraw(choiceContext, this, cardsToDraw);
    }
}
