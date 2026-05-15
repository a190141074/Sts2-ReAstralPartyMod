using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class TwinShadowsPower : AstralPartyPowerModel
{
    private const decimal ShadowDamage = 1m;

    public override PowerType Type => PowerType.Debuff;

    // Each shadow uses its own timer, so new applications stay separate.
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool IsInstanced => true;

    public override int DisplayAmount => Amount;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || side != Owner.Side)
            return;
        if (Amount <= 0)
            return;

        Flash();
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner,
            ShadowDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Applier,
            null
        );

        if (Owner.IsAlive)
            await PowerCmd.TickDownDuration(this);
    }
}
