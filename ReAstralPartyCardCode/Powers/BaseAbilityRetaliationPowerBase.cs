using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class BaseAbilityRetaliationPowerBase : AstralPartyPowerModel
{
    private sealed class Data
    {
        public bool IsReflecting;
        public PowerModel? PendingPower;
        public decimal PendingAmount;
        public Creature? PendingApplier;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (target != Owner || Amount <= 0m || amount <= 0m)
            return false;
        if (canonicalPower.Type != PowerType.Debuff)
            return false;

        var data = GetInternalData<Data>();
        if (data.IsReflecting)
            return false;

        data.PendingPower = canonicalPower;
        data.PendingAmount = amount;
        data.PendingApplier = applier;
        return false;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        var data = GetInternalData<Data>();
        if (data.IsReflecting)
            return;
        if (data.PendingPower == null || data.PendingAmount <= 0m)
            return;
        if (power != data.PendingPower && power != this)
            return;
        if (data.PendingApplier == null || !data.PendingApplier.IsAlive || data.PendingApplier == Owner)
        {
            ClearPending(data);
            return;
        }
        if (applier != data.PendingApplier && applier != null)
        {
            ClearPending(data);
            return;
        }

        var target = ResolveRetaliationTarget(data.PendingApplier);
        if (target == null || !target.IsAlive)
        {
            ClearPending(data);
            return;
        }

        var pendingPower = data.PendingPower;
        var pendingAmount = data.PendingAmount;
        var pendingApplier = data.PendingApplier;
        if (pendingPower == null || pendingAmount <= 0m || pendingApplier == null)
        {
            ClearPending(data);
            return;
        }

        data.IsReflecting = true;
        try
        {
            Flash();
            await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
            await PowerCmd.Apply(pendingPower.ToMutable(), target, pendingAmount, Owner, cardSource, false);
        }
        finally
        {
            data.IsReflecting = false;
            ClearPending(data);
        }
    }

    protected abstract Creature? ResolveRetaliationTarget(Creature source);

    private static void ClearPending(Data data)
    {
        data.PendingPower = null;
        data.PendingAmount = 0m;
        data.PendingApplier = null;
    }
}
