using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillFateFirewoodStickYi : AstralPartyCardModel
{
    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override string PortraitBasePath => "res://ReAstralPartyMod/images/card_portraits/skill_fate_firewood_stick";

    protected override string FrameBasePath => PortraitBasePath;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, AstralKeywords.FateFirewoodStickDuel];

    protected override bool IsPlayable => FateFirewoodStickCombatHelper.HasLivingEnemyTarget(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<FateFirewoodNodePower>()
    ];

    public SkillFateFirewoodStickYi() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();

        var ownerCreature = Owner?.Creature;
        var target = cardPlay.Target;
        if (ownerCreature == null || target == null || !target.IsAlive || target.Side == ownerCreature.Side)
            return;
        if (Owner?.GetRelic<VariantPersonManosabaLinHiro>() is not { } hiroRelic)
            return;

        await FateFirewoodStickCombatHelper.ResolveDuelAsync(choiceContext, this, target, hiroRelic, false);
    }
}
