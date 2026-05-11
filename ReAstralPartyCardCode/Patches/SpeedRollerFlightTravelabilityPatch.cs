using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class SpeedRollerFlightTravelabilityPatch
{
    public static void Postfix(NMapScreen __instance)
    {
        var runStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
        if (runStateField?.GetValue(__instance) is not RunState runState)
            return;

        if (!TryGetAnyFlightCharges(runState, out _))
            return;
        if (runState.Map == null || runState.VisitedMapCoords.Count == 0)
            return;

        var mapPointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
        if (mapPointDictionaryField?.GetValue(__instance) is not IDictionary<MapCoord, NMapPoint> mapPointDictionary)
            return;

        var currentCoord = runState.VisitedMapCoords[^1];
        var nextRow = currentCoord.row + 1;
        if (nextRow >= runState.Map.GetRowCount())
            return;

        foreach (var point in runState.Map.GetPointsInRow(nextRow))
        {
            if (mapPointDictionary.TryGetValue(point.coord, out var mapPoint))
                mapPoint.State = MapPointState.Travelable;
        }
    }

    internal static bool TryGetAnyFlightCharges(RunState runState, out SpeedRollerRelicBase? relic)
    {
        foreach (var player in runState.Players)
        {
            foreach (var candidate in player.Relics)
            {
                if (candidate is SpeedRollerRelicBase speedRoller && speedRoller.HasFlightCharge())
                {
                    relic = speedRoller;
                    return true;
                }
            }
        }

        relic = null;
        return false;
    }
}
