using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class JingRuiPower : AstralPartyPowerModel
{
    private const decimal MaxStacks = 12m;
    private const decimal BonusPercentPerStack = 0.05m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Math.Clamp(Amount, 0m, MaxStacks);

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not JingRuiPower)
            return false;
        if (target != Owner || amount <= 0m)
            return false;

        modifiedAmount = Math.Clamp(amount, 0m, Math.Max(MaxStacks - Math.Clamp(Amount, 0m, MaxStacks), 0m));
        return true;
    }

    public static decimal GetVigilDamageBonus(Creature? owner, decimal amount)
    {
        if (owner == null || amount <= 0m)
            return 0m;

        var stacks = Math.Clamp(owner.GetPowerAmount<JingRuiPower>(), 0m, MaxStacks);
        return stacks <= 0m ? 0m : amount * stacks * BonusPercentPerStack;
    }
}
