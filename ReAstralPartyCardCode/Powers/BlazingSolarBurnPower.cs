using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class BlazingSolarBurnPower : AstralPartyPowerModel
{
    private const decimal MaxStackDamage = 50m;
    private const decimal MaxHpDamageRatio = 0.04m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max(0, Convert.ToInt32(decimal.Round(Amount, 0, MidpointRounding.AwayFromZero)));

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player.Creature != Owner)
            return;
        if (Amount <= 0m)
            return;

        var baseDamage = Math.Min(Amount, MaxStackDamage);
        var capDamage = Math.Ceiling(Owner.MaxHp * MaxHpDamageRatio);
        var finalDamage = Math.Max(1m, Math.Min(baseDamage, capDamage));
        await CreatureCmd.Damage(choiceContext, Owner, finalDamage, ValueProp.Move, null, null);
        await PowerCmd.SetAmount<BlazingSolarBurnPower>(Owner, Math.Max(0m, Amount - 1m), null, null);
    }
}
