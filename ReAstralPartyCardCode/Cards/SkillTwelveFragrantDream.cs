using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillTwelveFragrantDream : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<JingRuiPower>(),
        HoverTipFactory.FromPower<CeremonialBombPower>(),
        HoverTipFactory.FromPower<FallenFlowerPower>()
    ];

    public SkillTwelveFragrantDream() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null || cardPlay.Target == null)
            return;

        var fixedDamage = Owner.Creature.GetPowerAmount<JingRuiPower>() * 6m;
        if (fixedDamage > 0m)
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, fixedDamage, ValueProp.Unpowered, Owner.Creature,
                this);

        if (cardPlay.Target.IsAlive)
        {
            await CreatureCmd.Stun(cardPlay.Target);
            await PowerCmd.Apply<FallenFlowerPower>(cardPlay.Target, 2m, Owner.Creature, this, false);
        }

        await PowerCmd.Apply<CeremonialBombPower>(Owner.Creature, 1m, Owner.Creature, this, false);
    }
}
