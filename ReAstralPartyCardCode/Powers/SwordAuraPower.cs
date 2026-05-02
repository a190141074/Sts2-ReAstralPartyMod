using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class SwordAuraPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner) return 0m;
        if (target == null || target.Side == Owner.Side) return 0m;
        if (cardSource?.Type != CardType.Attack) return 0m;

        return Amount switch
        {
            >= 3 => 4m,
            2 => 2m,
            1 => 1m,
            _ => 0m
        };
    }
}