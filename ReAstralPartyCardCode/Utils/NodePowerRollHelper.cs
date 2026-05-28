using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class NodePowerRollHelper
{
    public static int RollNodeValue(Player player, AbstractModel source, string nodeKey, int minValue, int maxValue)
    {
        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            minValue,
            maxValue + 1,
            MainFile.ModId,
            source.Id.Entry,
            nodeKey,
            player.RunState?.Rng?.StringSeed,
            player.NetId,
            player.Creature?.CombatState?.RoundNumber ?? 0);
    }
}
