using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class VoidCrystalPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public bool IsActivated;
    }

    private const decimal DefaultDelay = 3m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => GetInternalData<Data>().IsActivated ? 0 : (int)Amount;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not VoidCrystalPower)
            return false;
        if (target != Owner)
            return false;

        // Unique delayed protocol: reapplications do nothing.
        modifiedAmount = 0m;
        return true;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount > 0m)
            return;

        await PowerCmd.ModifyAmount(this, DefaultDelay, applier, cardSource, true);
    }

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        if (Owner?.Player != player)
            return amount;

        return GetInternalData<Data>().IsActivated ? amount + 1m : amount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        var data = GetInternalData<Data>();
        if (data.IsActivated)
            return;
        if (Amount > 1m)
        {
            await PowerCmd.Decrement(this);
            return;
        }

        if (Amount <= 0m)
            return;

        data.IsActivated = true;
        await PowerCmd.ModifyAmount(this, -Amount, Owner, null, true);
        Flash();
        InvokeDisplayAmountChanged();
    }
}