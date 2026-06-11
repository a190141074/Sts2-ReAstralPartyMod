using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class WaterWrapPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal PendingConsumedAmount;
        public bool PendingHeal;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => StableNumericStateHelper.RoundToNonNegativeInt(Amount);

    public override bool ShouldReceiveCombatHooks => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return amount;
        if (amount <= 0m || Amount <= 0m)
            return amount;

        var consumedAmount = decimal.Min(amount, Amount);
        if (consumedAmount <= 0m)
            return amount;

        var data = GetInternalData<Data>();
        data.PendingConsumedAmount += consumedAmount;
        data.PendingHeal = true;
        Flash();
        return amount - consumedAmount;
    }

    public override async Task AfterModifyingHpLostBeforeOsty()
    {
        if (Owner == null)
            return;

        var data = GetInternalData<Data>();
        var consumedAmount = decimal.Min(data.PendingConsumedAmount, Amount);
        var shouldHeal = data.PendingHeal && consumedAmount > 0m;
        data.PendingConsumedAmount = 0m;
        data.PendingHeal = false;

        if (consumedAmount > 0m)
        {
            var remainingAmount = Amount - consumedAmount;
            if (remainingAmount <= 0m)
                await PowerCmd.Remove(this);
            else
                await PowerCmd.ModifyAmount(this, -consumedAmount, Owner, null, true);
        }

        if (shouldHeal && Owner.IsAlive)
            await CreatureCmd.Heal(Owner, 1m, true);
    }
}
