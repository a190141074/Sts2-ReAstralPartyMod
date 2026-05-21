using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralMoveAgainDisplayHelper
{
    public static async Task Sync(Player? owner)
    {
        if (owner?.Creature == null)
            return;

        var desiredAmount = Math.Max(GetPendingCount(owner), 0);
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
        if (owner == null)
            return 0;

        return owner.Relics.OfType<VariantPersonSara>().Sum(relic => relic.GetPendingExtraTurnCount())
               + owner.Relics.OfType<JewelryNightSkin>().Sum(relic => relic.GetPendingExtraTurnCount());
    }
}
