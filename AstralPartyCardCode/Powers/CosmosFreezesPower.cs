using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class CosmosFreezesPower : AstralPartyPowerModel
{
    private const decimal BaseReductionDenominator = 100m;

    private sealed class Data
    {
        public int DisplayReductionAmount;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => GetInternalData<Data>().DisplayReductionAmount;

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

        if (canonicalPower is not CosmosFreezesPower)
            return false;
        if (target != Owner)
            return false;
        return false;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner)
            return 0m;
        if (amount <= 0m || Amount <= 0m)
            return 0m;

        var reducedDamage = amount * BaseReductionDenominator / (BaseReductionDenominator + Amount);
        var reductionAmount = Math.Clamp(amount - reducedDamage, 0m, amount);

        UpdateDisplayReduction(reductionAmount);
        return -reductionAmount;
    }

    public override Task AfterModifyingDamageAmount(CardModel? cardSource)
    {
        if (cardSource == null)
            UpdateDisplayReduction(0m);

        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;
        if (Amount <= 10m)
            return;

        var decayAmount = Math.Max(1m, Math.Floor(Amount * 0.10m));
        var newAmount = Math.Max(Amount - decayAmount, 0m);
        var delta = newAmount - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);

        if (newAmount <= 0m)
            await PowerCmd.Remove(this);
    }

    private void UpdateDisplayReduction(decimal reductionAmount)
    {
        var displayReduction = reductionAmount <= 0m ? 0 : (int)Math.Ceiling(reductionAmount);
        var data = GetInternalData<Data>();
        if (data.DisplayReductionAmount == displayReduction)
            return;

        data.DisplayReductionAmount = displayReduction;
        InvokeDisplayAmountChanged();
    }
}