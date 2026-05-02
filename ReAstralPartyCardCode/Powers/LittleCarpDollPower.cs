using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class LittleCarpDollPower : AstralPartyPowerModel
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StrengthPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not LittleCarpDollPower || target != Owner || amount <= 0m)
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
        await PowerCmd.Apply<DexterityPower>(Owner, addedAmount, applier, cardSource, true);
    }
}