using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class TsunamiPower : AstralPartyPowerModel
{
    private const decimal DamageReductionPercent = 0.16m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => (int)Amount;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m || Amount <= 0m)
            return 0m;
        if (dealer == null || dealer.Side == Owner?.Side)
            return 0m;
        if (dealer.GetPowerAmount<WeakPower>() <= 0m)
            return 0m;

        return -amount * DamageReductionPercent;
    }
}
