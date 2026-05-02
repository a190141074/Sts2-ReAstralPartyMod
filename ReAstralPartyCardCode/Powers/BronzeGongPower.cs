using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
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
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount <= 0m)
            return;

        var holographicPower = Owner.GetPower<ReversedScalesHolographicPower>();
        if (holographicPower != null)
        {
            if (holographicPower.Amount <= Amount)
                await PowerCmd.Remove(holographicPower);
            else
                await PowerCmd.ModifyAmount(holographicPower, -Amount, Owner, null, true);
        }

        await PowerCmd.Remove(this);
    }
}