using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillProductRestocking : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralPartyMod.AstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

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

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var discardedCardCount = handCards.Count;

        // This card leaves the hand before its effect resolves, so add it back when calculating
        // how many cards to draw from the player's original hand size.
        var cardsToDraw = discardedCardCount + 1;

        if (discardedCardCount > 0)
            await CardCmd.DiscardAndDraw(choiceContext, handCards, cardsToDraw);
        else
            await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);

        if (discardedCardCount <= 0 || Owner.GetRelic<PersonMascotGirlMimi>() == null)
            return;

        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            discardedCardCount,
            Owner.Creature,
            this,
            false
        );
    }
}