using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

/// <summary>
/// Heals at the start of the owner's next turn, then halves its amount.
/// </summary>
public class HalfLifeHealPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || Owner.Player != player)
            return;

        var healAmount = AmountOnTurnStart;
        if (healAmount <= 0)
            return;

        Flash();
        await CreatureCmd.Heal(Owner, healAmount, true);

        var newAmount = healAmount / 2;
        if (newAmount <= 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var delta = newAmount - Amount;
        if (delta != 0)
            await PowerCmd.ModifyAmount(this, delta, Owner, null, true);
    }
}