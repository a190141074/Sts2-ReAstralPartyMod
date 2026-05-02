using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class MouseShieldPower : AstralPartyPowerModel
{
    private class Data
    {
        public bool ShouldRemove;
    }

    private const decimal DamageReduction = 999m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override bool TryModifyPowerAmountReceived(PowerModel canonicalPower, Creature target, decimal amount,
        Creature? applier, out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        // Keep the power single-instance even if something tries to apply it again.
        if (canonicalPower is not MouseShieldPower) return false;
        if (target != Owner) return false;
        if (amount <= 0m) return false;

        modifiedAmount = 0m;
        return true;
    }

    public override decimal ModifyHpLostBeforeOstyLate(Creature target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (target != Owner) return amount;
        if (amount <= 0m) return amount;

        GetInternalData<Data>().ShouldRemove = true;
        return Math.Max(amount - DamageReduction, 0m);
    }

    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        var data = GetInternalData<Data>();
        if (!data.ShouldRemove)
            return;

        data.ShouldRemove = false;
        Flash();
        await PowerCmd.Remove(this);
    }
}