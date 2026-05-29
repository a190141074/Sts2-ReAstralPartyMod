using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralMoveAgainDisplayHelper
{
    public static async Task Sync(Player? owner)
    {
        if (owner?.Creature == null)
            return;

        var desiredAmount = PendingExtraTurnQueuePower.GetPendingCount(owner);
        if (desiredAmount <= 0)
        {
            var existingPower = owner.Creature.GetPower<MoveAgainPower>();
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);
            return;
        }

        var currentPower = owner.Creature.GetPower<MoveAgainPower>();
        if (currentPower == null)
        {
            await PowerCmd.Apply<MoveAgainPower>(owner.Creature, desiredAmount, owner.Creature, null, false);
            return;
        }

        if ((int)currentPower.Amount != desiredAmount)
            await PowerCmd.SetAmount<MoveAgainPower>(owner.Creature, desiredAmount, owner.Creature, null);
    }

    public static int GetPendingCount(Player? owner)
    {
        return PendingExtraTurnQueuePower.GetPendingCount(owner);
    }
}
