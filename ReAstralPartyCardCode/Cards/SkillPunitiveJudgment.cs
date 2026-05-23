using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillPunitiveJudgment : AstralPartyCardModel
{
    private const decimal BaseDamage = 21m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<BlazingSolarBurnPower>(),
        HoverTipFactory.FromPower<RageOfFirePower>()
    ];

    public SkillPunitiveJudgment() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card != this || !IsUpgraded || card.EnergyCost.CostsX)
            return false;

        modifiedCost = 1m;
        return originalCost != modifiedCost;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (cardSource != this)
            return 0m;
        if (target == null || amount <= 0m)
            return 0m;

        var multiplier = AstralSinkouHelper.GetPunitiveJudgmentDamageMultiplier(target);
        return Math.Max(0m, amount * multiplier - amount);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null)
            return;

        var target = cardPlay.Target;
        if (target == null || target.Side == Owner.Creature.Side || !target.IsAlive)
            return;

        var multiplier = AstralSinkouHelper.GetPunitiveJudgmentDamageMultiplier(target);
        var baseDamage = DynamicVars["Damage"].BaseValue * multiplier;
        var extraDamage = AstralSinkouHelper.GetPunitiveJudgmentExtraDamage(Owner, target);

        await CreatureCmd.Damage(choiceContext, target, baseDamage, ValueProp.Move, Owner.Creature, this);
        await PowerCmd.Apply<BlazingSolarBurnPower>(target, 1m, Owner.Creature, this, false);
        await CreatureCmd.Damage(
            choiceContext,
            target,
            extraDamage,
            ValueProp.Unpowered | ValueProp.Unblockable,
            Owner.Creature,
            null);
    }
}
