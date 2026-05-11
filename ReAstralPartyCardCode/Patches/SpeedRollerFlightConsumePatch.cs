using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class SpeedRollerFlightConsumePatch
{
    private static readonly FieldInfo? PlayerField = AccessTools.Field(typeof(MoveToMapCoordAction), "_player");
    private static readonly FieldInfo? DestinationField = AccessTools.Field(typeof(MoveToMapCoordAction), "_destination");

    public static void Prefix(MoveToMapCoordAction __instance)
    {
        if (PlayerField?.GetValue(__instance) is not MegaCrit.Sts2.Core.Entities.Players.Player player)
            return;
        if (DestinationField?.GetValue(__instance) is not MapCoord destination)
            return;

        foreach (var relic in player.Relics)
        {
            if (relic is not SpeedRollerRelicBase speedRoller)
                continue;

            if (speedRoller.TryConsumeFlightChargeForDestination(destination))
                return;
        }
    }
}
