using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class ArtKnifeRelicBase : AstralPartyRelicModel
{
    protected abstract decimal StrengthBonus { get; }
    protected abstract CardType DamageCardType { get; }
    protected virtual decimal HealDamageDivisor => 1m;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply<ArtKnifeFullHpStrengthPower>(
            Owner.Creature,
            StrengthBonus,
            Owner.Creature,
            null,
            true);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!IsAtFullHp())
            return 0m;
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (!WarforgeEnchantmentHelper.MatchesCardType(cardSource, DamageCardType))
            return 0m;

        return Owner.Creature.GetPowerAmount<HalfLifeHealPower>() / HealDamageDivisor;
    }

    private bool IsAtFullHp()
    {
        return ArtKnifeActivationHelper.IsActivationSatisfied(Owner?.Creature);
    }
}
