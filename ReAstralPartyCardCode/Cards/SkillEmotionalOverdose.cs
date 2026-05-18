using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillEmotionalOverdose : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<LovePower>(),
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>()
    ];

    public SkillEmotionalOverdose() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var loveStacks = Math.Max((int)Owner.Creature.GetPowerAmount<LovePower>(), 0);
        var needyDerivative = Owner.GetRelic<PersonalityDerivativeNeedyGirl>();
        if (loveStacks >= PersonalityDerivativeNeedyGirl.BaseLoveCap && needyDerivative != null)
            await needyDerivative.GainPermanentGrowth(Owner.Creature);

        if (loveStacks > 0)
        {
            await PowerCmd.Apply<HalfLifeHealPower>(Owner.Creature, loveStacks, Owner.Creature, this, false);
            await AstralTemporaryDexterityPower.Apply(Owner.Creature, loveStacks, this, Owner.Creature, this, true);
        }

        var lovePower = Owner.Creature.GetPower<LovePower>();
        if (lovePower != null)
            await PowerCmd.Remove(lovePower);
    }
}

