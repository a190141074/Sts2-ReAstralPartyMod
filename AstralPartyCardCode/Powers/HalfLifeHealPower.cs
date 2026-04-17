using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

/// <summary>
/// Heals at the start of the owner's side turn, then halves its amount.
/// </summary>
public class HalfLifeHealPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || side != Owner.Side)
            return;

        if (Amount <= 0)
            return;

        var healAmount = Amount;

        Flash();
        await CreatureCmd.Heal(Owner, healAmount, true);

        // Round up so 3 becomes 2 and 1 stays 1.
        var newAmount = Math.Max((int)Math.Ceiling(Amount / 2m), 0);
        var delta = newAmount - Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);
    }
}
