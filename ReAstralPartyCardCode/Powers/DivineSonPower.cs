using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class DivineSonPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target == null || amount <= 0m)
            return 0m;

        if (target == Owner)
            return dealer != null && dealer.Side != Owner.Side ? 1m : 0m;

        if (dealer != Owner || target.Side == Owner.Side)
            return 0m;

        var bonus = 1m;
        if (WarforgeEnchantmentHelper.CountsAsAttack(cardSource) && Owner.Player != null
            && AstralDivinePersonaHelper.GetStrongestBookOfHeavenStacks(Owner.Player) >= 7)
            bonus += 1m;

        return bonus;
    }
}
