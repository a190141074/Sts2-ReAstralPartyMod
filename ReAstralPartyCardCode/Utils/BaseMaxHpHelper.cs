using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class BaseMaxHpHelper
{
    public static decimal GetBaseMaxHp(Player? player)
    {
        if (player?.Character != null)
            return Math.Max(0m, player.Character.StartingHp);

        return Math.Max(0m, player?.Creature?.MaxHp ?? 0m);
    }

    public static decimal GetBaseMaxHp(Creature? creature)
    {
        if (creature?.Player?.Character != null)
            return Math.Max(0m, creature.Player.Character.StartingHp);

        return Math.Max(0m, creature?.MaxHp ?? 0m);
    }
}
