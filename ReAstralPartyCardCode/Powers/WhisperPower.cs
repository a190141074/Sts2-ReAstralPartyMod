using System;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class WhisperPower : AstralPartyPowerModel
{
    private const decimal MaxStacks = 4m;
    private const decimal DamagePercentPerStack = 0.12m;
    private const decimal DamageFlatPerStack = 1m;
    private const decimal HealReductionPerStack = 0.2m;
    private const decimal BlockReductionPerStack = 0.1m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => (int)GetClampedStacks();

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not WhisperPower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount = Math.Clamp(amount, 0m, Math.Max(MaxStacks - GetClampedStacks(), 0m));
        return true;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var stacks = GetClampedStacks();
        if (stacks <= 0m || amount <= 0m || Owner == null)
            return 0m;

        if (target == Owner)
            return stacks;

        if (dealer != Owner || target == null || target.Side == Owner.Side)
            return 0m;

        return amount * DamagePercentPerStack * stacks + DamageFlatPerStack * stacks;
    }

    public static decimal AdjustHealAmount(Creature? target, decimal amount)
    {
        if (target == null || amount <= 0m)
            return amount;

        var stacks = GetClampedStacks(target);
        if (stacks <= 0m)
            return amount;

        return Math.Max(0m, amount * Math.Max(0m, 1m - stacks * HealReductionPerStack));
    }

    public static decimal AdjustBlockAmount(Creature? target, decimal amount)
    {
        if (target == null || amount <= 0m)
            return amount;

        var stacks = GetClampedStacks(target);
        if (stacks <= 0m)
            return amount;

        return Math.Max(0m, amount * Math.Max(0m, 1m - stacks * BlockReductionPerStack));
    }

    private decimal GetClampedStacks()
    {
        return GetClampedStacks(Owner);
    }

    private static decimal GetClampedStacks(Creature? target)
    {
        return Math.Clamp(target?.GetPowerAmount<WhisperPower>() ?? 0m, 0m, MaxStacks);
    }
}
