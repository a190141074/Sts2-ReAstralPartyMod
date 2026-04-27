using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
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

    public SkillVampireBite() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        if (Owner.Creature.MaxHp > 0m && Owner.Creature.CurrentHp > Owner.Creature.MaxHp * 0.5m)
            await CreatureCmd.Damage(choiceContext, Owner.Creature, SelfDamageAmount,
                ValueProp.Unblockable | ValueProp.Unpowered, this);

        await PowerCmd.Apply<BloodthirstPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
