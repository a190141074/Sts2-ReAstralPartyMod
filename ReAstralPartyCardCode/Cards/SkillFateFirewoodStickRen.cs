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

[RegisterCard(typeof(PersonaSkillCardPool))]
public sealed class SkillFateFirewoodStickRen : AstralPartyCardModel
{
    protected override bool ShouldAutoApplyCooldownEnchantment => false;

    protected override string PortraitBasePath => "res://ReAstralPartyMod/images/card_portraits/skill_fate_firewood_stick";

    protected override string FrameBasePath => PortraitBasePath;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, AstralKeywords.FateFirewoodStickDuel];

    protected override bool IsPlayable => BaseAbilityHelper.HasOtherLivingPlayerTarget(Owner);

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>(),
        HoverTipFactory.FromPower<FateFirewoodNodePower>()
    ];

    public SkillFateFirewoodStickRen() : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AnyAlly, showInCardLibrary: false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();

        var owner = Owner;
        var target = cardPlay.Target;
        if (owner == null || target == null)
            return;
        if (!BaseAbilityHelper.IsOtherLivingPlayerTarget(owner, target))
            return;
        if (owner.GetRelic<VariantPersonManosabaLinHiro>() is not { } hiroRelic)
            return;

        await FateFirewoodStickCombatHelper.ResolveDuelAsync(choiceContext, this, target, hiroRelic, true);
    }
}
