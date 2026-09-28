using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralPassingThroughHelper
{
    public static bool IsPassingThrough(Player owner, Player currentPlayer, int node)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null || currentPlayer == null)
            return false;

        var orderedPlayers = combatState.Players
            .Where(player => player.Creature != null)
            .OrderBy(player => player.NetId)
            .ToList();
        var playerIndex = orderedPlayers.FindIndex(player => player.NetId == currentPlayer.NetId);
        if (playerIndex < 0)
            return false;

        return playerIndex % 10 == node % 10;
    }
}
