using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class GiantAnchorPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal ProcessedAmount;
        public decimal PendingAddedAmount;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

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

        if (canonicalPower is not GiantAnchorPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
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

        await PowerCmd.Apply<StrengthPower>(Owner, addedAmount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount <= 0m)
            return;

        await PowerCmd.TickDownDuration(this);
        await PowerCmd.Apply<StrengthPower>(Owner, -1m, Owner, null, true);

        var data = GetInternalData<Data>();
        data.ProcessedAmount = Math.Max(0m, Amount - 1m);
    }
}