using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class ReversedScalesPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal ProcessedAmount;
        public decimal PendingAddedAmount;
    }

    private const decimal DamageReductionPerStack = 2m;
    private const decimal TemporaryStrengthPerStack = 2m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not ReversedScalesPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        GetInternalData<Data>().PendingAddedAmount += amount;
        return true;
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

        await BoundaryReinforcementPower.ApplyTemporaryStrength(
            Owner,
            addedAmount * TemporaryStrengthPerStack,
            applier,
            cardSource
        );
    }

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return amount;
        if (amount <= 0m || Amount <= 0m)
            return amount;

        return Math.Max(amount - Amount * DamageReductionPerStack, 0m);
    }
}
