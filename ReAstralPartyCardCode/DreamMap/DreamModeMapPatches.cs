using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamMap;

public sealed class DreamModeTravelabilityPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_travelability";

    public static string Description => "Allow dream-mode travelability to use undirected neighbors and revisits";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), "RecalculateTravelability")];
    }

    public static void Postfix(NMapScreen __instance)
    {
        var runStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
        if (runStateField?.GetValue(__instance) is not RunState runState)
            return;
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEnabled(runState))
            return;
        if (runState.Map == null)
            return;

        var mapPointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
        if (mapPointDictionaryField?.GetValue(__instance) is not IDictionary<MapCoord, NMapPoint> mapPointDictionary)
            return;

        if (runState.VisitedMapCoords.Count == 0)
            return;

        var currentPoint = runState.CurrentMapPoint;
        if (currentPoint == null)
            return;

        foreach (var neighbor in LucidDreamMaliceRuntimeHelper.GetDreamModeNeighbors(runState, currentPoint))
        {
            if (mapPointDictionary.TryGetValue(neighbor.coord, out var mapPoint))
                mapPoint.State = MapPointState.Travelable;
        }

        if (runState.CurrentMapCoord.HasValue
            && runState.Map.BossMapPoint != null
            && runState.CurrentMapCoord.Value == runState.Map.BossMapPoint.coord)
        {
            return;
        }
    }
}

public sealed class DreamModeTravelPathPatch : IPatchMethod
{
    private static readonly FieldInfo? RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo? PathsField = AccessTools.Field(typeof(NMapScreen), "_paths");

    public static string PatchId => "dream_mode_travel_path";

    public static string Description => "Resolve reverse dream-mode travel path animations by mirroring existing path ticks";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMapScreen), nameof(NMapScreen.TravelToMapCoord), [typeof(MapCoord)])];
    }

    public static bool Prefix(
        NMapScreen __instance,
        MapCoord coord,
        ref Task __result)
    {
        if (RunStateField?.GetValue(__instance) is not RunState runState)
            return true;
        if (!LucidDreamMaliceRuntimeHelper.IsDreamModeEnabled(runState))
            return true;
        if (PathsField?.GetValue(__instance) is not IDictionary<(MapCoord, MapCoord), IReadOnlyList<Godot.TextureRect>> paths)
            return true;
        if (runState.VisitedMapCoords.Count == 0)
            return true;

        var lastCoord = runState.VisitedMapCoords[^1];
        if (paths.ContainsKey((lastCoord, coord)) || !LucidDreamMaliceRuntimeHelper.TryResolveTravelPathTicks(paths, lastCoord, coord, out _))
            return true;

        paths[(lastCoord, coord)] = paths[(coord, lastCoord)];
        return true;
    }
}
