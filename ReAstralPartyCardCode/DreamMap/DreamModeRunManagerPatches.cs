using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamMap;

public sealed class DreamModeDuplicateVisitedMapCoordPatch : IPatchMethod
{
    private static readonly FieldInfo? VisitedMapCoordsField = AccessTools.Field(typeof(RunState), "_visitedMapCoords");
    private static readonly PropertyInfo? NextRoomIdProperty = AccessTools.Property(typeof(RunState), "NextRoomId");

    public static string PatchId => "dream_mode_duplicate_visited_map_coord";

    public static string Description => "Allow duplicate visited map coords under dream mode while keeping custom visit state";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunState), nameof(RunState.AddVisitedMapCoord), [typeof(MapCoord)])];
    }

    public static bool Prefix(RunState __instance, MapCoord coord, ref bool __result)
    {
        var modifier = LucidDreamMaliceModifier.Get(__instance);
        if (modifier?.EnableDreamMode != true)
            return true;

        if (VisitedMapCoordsField?.GetValue(__instance) is not List<MapCoord> visitedMapCoords)
            return true;

        var alreadyVisited = visitedMapCoords.Contains(coord);
        LucidDreamMaliceRuntimeHelper.RegisterDreamVisit(modifier, coord);
        if (!alreadyVisited)
            return true;

        visitedMapCoords.Add(coord);
        NextRoomIdProperty?.SetValue(__instance, 0);
        MainFile.Logger.Info(
            $"[DreamMode] Registered duplicate visited coord=({coord.col},{coord.row}) | visitedCount={visitedMapCoords.Count}.");
        __result = true;
        return false;
    }
}

public sealed class DreamModeEnterMapCoordPatch : IPatchMethod
{
    public static string PatchId => "dream_mode_enter_map_coord";

    public static string Description => "Only keep dream-mode revisit bookkeeping when entering a map coord";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), nameof(RunManager.EnterMapCoord), [typeof(MapCoord)])];
    }

    public static bool Prefix(RunManager __instance, MapCoord coord)
    {
        if (AccessTools.Property(typeof(RunManager), "State")?.GetValue(__instance) is not RunState runState)
            return true;
        var modifier = LucidDreamMaliceModifier.Get(runState);
        if (modifier?.EnableDreamMode != true)
            return true;

        var point = runState.Map?.GetPoint(coord);
        if (point == null)
            return true;

        LucidDreamMaliceRuntimeHelper.SetDreamModePendingRevisit(modifier, coord, point.PointType);
        MainFile.Logger.Info(
            $"[DreamMode] EnterMapCoord bookkeeping | coord=({coord.col},{coord.row}) | pointType={point.PointType} | priorVisits={LucidDreamMaliceRuntimeHelper.GetVisitCount(modifier, coord)}.");
        return true;
    }
}

public sealed class DreamModeCreateRoomPatch : IPatchMethod
{
    private static readonly PropertyInfo? RunManagerStateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    public static string PatchId => "dream_mode_create_room";

    public static string Description => "Route dream-mode revisits through CreateRoom instead of EnterMapCoord";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), "CreateRoom", [typeof(RoomType), typeof(MapPointType), typeof(AbstractModel)])];
    }

    public static bool Prefix(
        RunManager __instance,
        RoomType roomType,
        MapPointType mapPointType,
        AbstractModel? model,
        ref AbstractRoom __result)
    {
        if (RunManagerStateProperty?.GetValue(__instance) is not RunState runState)
            return true;

        var modifier = LucidDreamMaliceModifier.Get(runState);
        if (modifier?.EnableDreamMode != true)
            return true;
        if (runState.CurrentMapCoord is not MapCoord coord)
            return true;

        if (!LucidDreamMaliceRuntimeHelper.TryResolveDreamModeRoomType(
                runState,
                coord,
                roomType,
                mapPointType,
                model,
                out var resolvedRoom))
            return true;

        __result = resolvedRoom;
        MainFile.Logger.Info(
            $"[DreamMode] CreateRoom routed coord=({coord.col},{coord.row}) roomType={roomType} mapPointType={mapPointType} resolvedRoom={resolvedRoom.GetType().Name}.");
        return false;
    }
}
