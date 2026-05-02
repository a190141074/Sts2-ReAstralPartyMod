using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class DragonRoarWardPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return amount;
        if (amount <= 0m)
            return amount;
        if (!IsHostileSource(dealer, cardSource))
            return amount;

        Flash();
        return 0m;
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null)
            return;
        if (side == Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    private bool IsHostileSource(Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null)
            return dealer.Side != Owner?.Side;

        var cardOwnerCreature = cardSource?.Owner?.Creature;
        return cardOwnerCreature != null && cardOwnerCreature.Side != Owner?.Side;
    }
}