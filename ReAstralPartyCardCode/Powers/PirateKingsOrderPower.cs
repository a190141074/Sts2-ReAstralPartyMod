using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class PirateKingsOrderPower : AstralPartyPowerModel
{
    private const decimal DamageBonus = 1m;
    private const decimal DamageReduction = 1m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner || amount <= 0m || Amount <= 0m)
            return 0m;

        return DamageBonus;
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner || amount <= 0m || Amount <= 0m)
            return amount;

        if (dealer == null || dealer.Side == Owner.Side)
            return amount;

        return Math.Max(0m, amount - DamageReduction);
    }
}
