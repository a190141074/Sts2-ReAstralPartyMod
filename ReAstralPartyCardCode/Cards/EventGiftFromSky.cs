using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

// 5. 天降之物：给所有友方单位抽牌并获得星光
[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 7)]
public class EventGiftFromSky : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(1), new IntVar("StarLight", 3)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventGiftFromSky() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        foreach (var player in CombatState.Players)
        {
            await CardGainAttribution.RunWithSource(this,
                () => CardPileCmd.Draw(choiceContext, (int)DynamicVars["Cards"].BaseValue, player));
            await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), player.Creature,
                DynamicVars["StarLight"].BaseValue, Owner.Creature, this, false);
        }
    }
}
