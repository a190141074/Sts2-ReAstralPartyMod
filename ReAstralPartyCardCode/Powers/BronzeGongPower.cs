using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class BronzeGongPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal ProcessedAmount;
        public decimal PendingAddedAmount;
        public decimal AppliedHolographicAmount;
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

        if (canonicalPower is not BronzeGongPower || target != Owner || amount <= 0m)
            return false;

        GetInternalData<Data>().PendingAddedAmount += amount;
        return false;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var addedAmount = data.PendingAddedAmount > 0m
            ? data.PendingAddedAmount
            : Math.Max(Amount - data.ProcessedAmount, 0m);

        data.PendingAddedAmount = 0m;
        data.ProcessedAmount = Amount;

        if (addedAmount <= 0m)
            return;

        await AstralTemporaryStrengthPower.Apply(Owner, addedAmount, this, applier, cardSource);
        await PowerCmd.Apply<ReversedScalesHolographicPower>(Owner, addedAmount, applier, cardSource, false);
        data.AppliedHolographicAmount += addedAmount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Player == null || player != Owner.Player || Amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var holographicPower = Owner.GetPower<ReversedScalesHolographicPower>();
        if (holographicPower != null && data.AppliedHolographicAmount > 0m)
        {
            if (holographicPower.Amount <= data.AppliedHolographicAmount)
                await PowerCmd.Remove(holographicPower);
            else
                await PowerCmd.ModifyAmount(holographicPower, -data.AppliedHolographicAmount, Owner, null, true);
        }

        data.AppliedHolographicAmount = 0m;
        await PowerCmd.Remove(this);
    }
}
