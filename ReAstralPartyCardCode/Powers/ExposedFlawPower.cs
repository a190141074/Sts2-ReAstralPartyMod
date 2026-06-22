using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ExposedFlawPower : AstralPartyPowerModel
{
    private const decimal MaxStacks = 6m;

    public override PowerType Type => PowerType.Debuff;

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
        if (canonicalPower is not ExposedFlawPower)
            return false;
        if (target != Owner || amount <= 0m)
            return false;

        modifiedAmount = Math.Clamp(amount, 0m, Math.Max(MaxStacks - Amount, 0m));
        return true;
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || Owner.Side == side || Amount <= 0m)
            return;

        await PowerCmd.Remove(this);
    }
}
