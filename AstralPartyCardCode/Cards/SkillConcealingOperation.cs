using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillConcealingOperation : AstralPartyCardModel
{
    private const decimal MarkAmount = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InvestigationTargetPower>(),
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralInvestigationProgress)
    ];

    public SkillConcealingOperation() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (owner == null || ownerCreature == null || target == null || target.Side == ownerCreature.Side)
            return;

        await ConcealingInvestigationHelper.ApplyInvestigationTarget(target, owner, this);
        await PowerCmd.Apply<MarkLockPower>(target, MarkAmount, ownerCreature, this, false);

        var targetMarks = target.GetPowerAmount<MarkLockPower>();
        if (targetMarks > 0m)
        {
            await PowerCmd.Apply<StarLightPower>(ownerCreature, targetMarks * 2m, ownerCreature, this, false);
        }

        await ConcealingInvestigationHelper.TryTriggerTruthUnveiledOnSpecialTarget(choiceContext, owner, owner, this);
    }
}
