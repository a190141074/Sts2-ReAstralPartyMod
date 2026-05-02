using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillConcealingOperation : AstralPartyCardModel
{
    private const decimal MarkAmount = 2m;
    private const string Stage1PortraitPath = "res://ReAstralPartyMod/images/card_portraits/events_concealing_investigation_a.png";
    private const string Stage2PortraitPath = "res://ReAstralPartyMod/images/card_portraits/events_concealing_investigation_b.png";
    private const string Stage3PortraitPath = "res://ReAstralPartyMod/images/card_portraits/events_concealing_investigation_c.png";
    private const string TruthPortraitPath = "res://ReAstralPartyMod/images/card_portraits/events_concealing_investigation_d.png";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<InvestigationTargetPower>(),
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralInvestigationProgressId)
    ];

    public override string PortraitPath => GetStagePortraitPath();

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
            await PowerCmd.Apply<StarLightPower>(ownerCreature, targetMarks * 2m, ownerCreature, this, false);

        await ConcealingInvestigationHelper.TryTriggerTruthUnveiledOnSpecialTarget(choiceContext, owner, owner, this);
    }

    private string GetStagePortraitPath()
    {
        var stage = Owner?.GetRelic<PersonPoisonedApple>()?.GetCurrentInvestigationStage()
                    ?? ConcealingInvestigationHelper.InvestigationStage.Stage1;

        return stage switch
        {
            ConcealingInvestigationHelper.InvestigationStage.Stage1 => Stage1PortraitPath,
            ConcealingInvestigationHelper.InvestigationStage.Stage2 => Stage2PortraitPath,
            ConcealingInvestigationHelper.InvestigationStage.Stage3 => Stage3PortraitPath,
            _ => TruthPortraitPath
        };
    }
}
