using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class RainGracePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => Amount > 0m ? 1 : 0;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature target,
        decimal amount,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not RainGracePower)
            return false;
        if (target != Owner)
            return false;
        if (amount <= 0m)
            return false;

        modifiedAmount = Amount >= 1m ? 0m : decimal.Min(amount, 1m - Amount);
        return true;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;
        if (AttackCardCostHelper.GetPlayedCost(cardPlay) < 1)
            return;

        Flash();
        await PowerCmd.Apply<HalfLifeHealPower>(Owner, 1m, Owner, cardPlay.Card, false);
    }

    public override async Task BeforeFlushLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.CombatState == null || Owner.Player != player)
            return;
        if (!Hook.ShouldFlush(Owner.CombatState, player))
            return;

        await PowerCmd.Remove(this);
    }
}
