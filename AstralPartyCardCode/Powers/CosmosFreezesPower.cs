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

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

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
        if (amount <= 0m)
            return false;

        var cap = applier?.GetPowerAmount<StagnantCosmosPower>() ?? decimal.MaxValue;
        var remainingRoom = Math.Max(cap - Amount, 0m);
        modifiedAmount = Math.Min(amount, remainingRoom);
        return true;
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

        return amount * BaseReductionDenominator / (BaseReductionDenominator + Amount);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;
        if (Amount <= 0m)
            return;

        var decayAmount = Math.Max(1m, Math.Ceiling(Amount * 0.10m));
        var newAmount = Math.Max(Amount - decayAmount, 0m);
        var delta = newAmount - Amount;
        if (delta != 0m)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);

        if (newAmount <= 0m)
            await PowerCmd.Remove(this);
    }
}
