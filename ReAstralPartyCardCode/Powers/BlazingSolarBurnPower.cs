using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class BlazingSolarBurnPower : AstralPartyPowerModel
{
    private const decimal MaxStackDamage = 50m;
    private const decimal NormalMaxHpDamageRatio = 0.16m;
    private const decimal EliteBossMaxHpDamageRatio = 0.08m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => StableNumericStateHelper.RoundToNonNegativeInt(Amount);

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || Owner.Side != side)
            return;
        if (Amount <= 0m)
            return;

        var baseDamage = Math.Min(Amount, MaxStackDamage);
        var maxHpRatio = Owner.CombatState?.Encounter?.RoomType is RoomType.Elite or RoomType.Boss
            ? EliteBossMaxHpDamageRatio
            : NormalMaxHpDamageRatio;
        var capDamage = Math.Ceiling(Owner.MaxHp * maxHpRatio);
        var finalDamage = Math.Max(1m, Math.Min(baseDamage, capDamage));
        using (SevenCursesDebuffProtectionHelper.EnterDebuffDamageContext())
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner, finalDamage, ValueProp.Move, null, null);
        await PowerCmd.SetAmount<BlazingSolarBurnPower>(Owner, Math.Max(0m, Amount - 1m), null, null);
    }
}
