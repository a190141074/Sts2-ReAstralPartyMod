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
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillVampireBite : AstralPartyCardModel
{
    private const decimal SelfDamageAmount = 6m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BloodthirstPower>(),
        HoverTipFactory.FromPower<CuteIsJusticePower>()
    ];

    public SkillVampireBite() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
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

        if (LowHpStateHelper.IsAboveHalfHp(Owner.Creature))
            await CreatureCmd.Damage(choiceContext, Owner.Creature, SelfDamageAmount,
                ValueProp.Unblockable | ValueProp.Unpowered, this);

        await PowerCmd.Apply<BloodthirstPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}

