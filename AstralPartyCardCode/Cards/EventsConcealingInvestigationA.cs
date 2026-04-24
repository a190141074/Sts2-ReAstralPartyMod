using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class EventsConcealingInvestigationA : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<ConcealingPower>()
    ];

    public EventsConcealingInvestigationA() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return;

        await ConcealingInvestigationHelper.ApplyMarkToRandomEligibleEnemy(
            Owner,
            1m,
            allowElite: false,
            allowBoss: false,
            this
        );
        await ConcealingInvestigationHelper.ApplyConcealingToTriggerAndBonnie(Owner, this);
    }
}
