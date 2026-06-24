using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public abstract class BaseAbilityRetaliationPowerBase : AstralPartyPowerModel
{
    private sealed record PendingRetaliation(
        PowerModel Power,
        decimal Amount,
        Creature Target);

    private sealed class Data
    {
        public bool IsRedirecting;
        public bool IsProcessing;
        public Queue<PendingRetaliation> PendingRetaliations { get; } = new();
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
        if (data.IsRedirecting)
            return false;
        if (applier == null || !applier.IsAlive || applier == Owner)
            return false;

        var retaliationTarget = ResolveRetaliationTarget(applier);
        if (retaliationTarget == null || !retaliationTarget.IsAlive || retaliationTarget == Owner)
            return false;

        var pendingPower = GetCanonicalPower(canonicalPower);
        if (pendingPower == null)
            return false;

        data.PendingRetaliations.Enqueue(new PendingRetaliation(
            pendingPower,
            amount,
            retaliationTarget));
        SchedulePendingRetaliations(data);
        modifiedAmount = 0m;
        return true;
    }

    private void SchedulePendingRetaliations(Data data)
    {
        if (data.IsProcessing)
            return;

        data.IsProcessing = true;
        TaskHelper.RunSafely(ProcessPendingRetaliationsAsync(data));
    }

    private async Task ProcessPendingRetaliationsAsync(Data data)
    {
        try
        {
            await Task.Yield();

            while (data.PendingRetaliations.Count > 0)
            {
                if (Owner == null || !Owner.IsAlive || Amount <= 0m)
                {
                    data.PendingRetaliations.Clear();
                    return;
                }

                var pending = data.PendingRetaliations.Dequeue();
                if (!pending.Target.IsAlive)
                    continue;

                data.IsRedirecting = true;
                try
                {
                    Flash();
                    await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
                    await PowerCmd.Apply(pending.Power.ToMutable(), pending.Target, pending.Amount, Owner, null, false);
                }
                finally
                {
                    data.IsRedirecting = false;
                }
            }
        }
        finally
        {
            data.IsProcessing = false;
            if (data.PendingRetaliations.Count > 0)
                SchedulePendingRetaliations(data);
        }
    }

    protected abstract Creature? ResolveRetaliationTarget(Creature source);

    private static PowerModel? GetCanonicalPower(PowerModel? power)
    {
        if (power == null)
            return null;

        return power.IsMutable ? ModelDb.GetById<PowerModel>(power.Id) : power;
    }
}
