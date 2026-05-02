using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ZuoTeaCakePower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public decimal ProcessedAmount;
        public decimal PendingAddedAmount;
        public decimal AppliedStrengthThisTurn;
        public decimal AppliedDexterityThisTurn;
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

        if (canonicalPower is not ZuoTeaCakePower || target != Owner || amount <= 0m)
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

        data.AppliedStrengthThisTurn += addedAmount;
        data.AppliedDexterityThisTurn += addedAmount;
        await PowerCmd.Apply<StrengthPower>(Owner, addedAmount, applier, cardSource, true);
        await PowerCmd.Apply<DexterityPower>(Owner, addedAmount, applier, cardSource, true);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        if (data.AppliedStrengthThisTurn > 0m)
            await PowerCmd.Apply<StrengthPower>(Owner, -data.AppliedStrengthThisTurn, Owner, null, true);
        if (data.AppliedDexterityThisTurn > 0m)
            await PowerCmd.Apply<DexterityPower>(Owner, -data.AppliedDexterityThisTurn, Owner, null, true);

        await PowerCmd.Remove(this);

        data.ProcessedAmount = 0m;
        data.PendingAddedAmount = 0m;
        data.AppliedStrengthThisTurn = 0m;
        data.AppliedDexterityThisTurn = 0m;
    }
}