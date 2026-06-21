using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillRemoteIntrusion : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CyberKittyFirewallBypassPower>(),
        HoverTipFactory.FromPower<CyberKittyNodePower>()
    ];

    public SkillRemoteIntrusion() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        await CyberKittyCombatHelper.GainRandomAttackCardFreeThisTurn(choiceContext, Owner, this);
        await PowerCmd.Apply(
            ModelDb.Power<CyberKittyFirewallBypassPower>().ToMutable(),
            Owner.Creature,
            1m,
            Owner.Creature,
            this,
            false);
    }
}

