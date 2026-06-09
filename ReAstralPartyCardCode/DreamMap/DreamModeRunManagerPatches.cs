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
    private static readonly PropertyInfo? RunManagerStateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    private static readonly MethodInfo? EnterMapCoordInternalMethod =
        AccessTools.Method(
            typeof(RunManager),
            "EnterMapCoordInternal",
            [typeof(MapCoord), typeof(AbstractRoom), typeof(bool)]);
    private static readonly MethodInfo? LoadIntoLatestMapCoordMethod =
        AccessTools.Method(typeof(RunManager), "LoadIntoLatestMapCoord", [typeof(AbstractRoom)]);

    private static readonly MethodInfo? CombatRoomMarkPreFinishedMethod =
        AccessTools.Method(typeof(CombatRoom), "MarkPreFinished");

    private static readonly FieldInfo? EncounterShouldGiveRewardsField =
        AccessTools.Field(typeof(EncounterModel), "<ShouldGiveRewards>k__BackingField");

    public static string PatchId => "dream_mode_enter_map_coord";

    public static string Description => "Route dream-mode revisits through custom room semantics";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), nameof(RunManager.EnterMapCoord), [typeof(MapCoord)])];
    }

    public static bool Prefix(RunManager __instance, MapCoord coord, ref Task __result)
    {
        if (RunManagerStateProperty?.GetValue(__instance) is not RunState runState)
            return true;
        var modifier = LucidDreamMaliceModifier.Get(runState);
        if (modifier?.EnableDreamMode != true)
            return true;

        var visitCountBeforeMove = LucidDreamMaliceRuntimeHelper.GetVisitCount(modifier, coord);
        var currentPoint = runState.Map?.GetPoint(coord);
        if (visitCountBeforeMove <= 0 || currentPoint == null)
        {
            LucidDreamMaliceRuntimeHelper.SetDreamModeRevisitedRestSite(modifier, false);
            return true;
        }

        MainFile.Logger.Info(
            $"[DreamMode] Revisit requested | coord=({coord.col},{coord.row}) | pointType={currentPoint.PointType} | priorVisits={visitCountBeforeMove}.");
        __result = HandleDreamModeRevisitAsync(__instance, runState, modifier, coord, currentPoint);
        return false;
    }

    private static async Task HandleDreamModeRevisitAsync(
        RunManager runManager,
        RunState runState,
        LucidDreamMaliceModifier modifier,
        MapCoord coord,
        MapPoint point)
    {
        runState.AddVisitedMapCoord(coord);
        MainFile.Logger.Info(
            $"[DreamMode] Handling revisit | coord=({coord.col},{coord.row}) | currentMapCoord=({runState.CurrentMapCoord?.col},{runState.CurrentMapCoord?.row}) | pointType={point.PointType}.");

        switch (point.PointType)
        {
            case MapPointType.Shop:
                LucidDreamMaliceRuntimeHelper.SetDreamModeRevisitedRestSite(modifier, false);
                MainFile.Logger.Info($"[DreamMode] Reopening revisited shop at coord=({coord.col},{coord.row}).");
                await InvokeLoadIntoLatestMapCoordAsync(runManager, new MerchantRoom());
                return;
            case MapPointType.RestSite:
                LucidDreamMaliceRuntimeHelper.SetDreamModeRevisitedRestSite(modifier, true);
                MainFile.Logger.Info($"[DreamMode] Reopening revisited rest site at coord=({coord.col},{coord.row}) with healing suppression.");
                await InvokeLoadIntoLatestMapCoordAsync(runManager, new RestSiteRoom());
                return;
            case MapPointType.Monster:
            case MapPointType.Elite:
            case MapPointType.Boss:
                LucidDreamMaliceRuntimeHelper.SetDreamModeRevisitedRestSite(modifier, false);
                if (TryCreatePreFinishedCombatRoom(runState, coord, point, out var combatRoom))
                {
                    MainFile.Logger.Info(
                        $"[DreamMode] Reopening cleared combat room at coord=({coord.col},{coord.row}) | pointType={point.PointType} | encounter={combatRoom.ModelId}.");
                    await InvokeLoadIntoLatestMapCoordAsync(runManager, combatRoom);
                    return;
                }

                MainFile.Logger.Warn(
                    $"[DreamMode] Combat revisit fallback triggered at coord=({coord.col},{coord.row}) because pre-finished room reconstruction failed. Loading map room instead.");
                await InvokeLoadIntoLatestMapCoordAsync(runManager, new MapRoom());
                return;
            default:
                LucidDreamMaliceRuntimeHelper.SetDreamModeRevisitedRestSite(modifier, false);
                MainFile.Logger.Info(
                    $"[DreamMode] Revisit for pointType={point.PointType} at coord=({coord.col},{coord.row}) will load an empty map room.");
                await InvokeLoadIntoLatestMapCoordAsync(runManager, new MapRoom());
                return;
        }
    }

    private static bool TryCreatePreFinishedCombatRoom(
        RunState runState,
        MapCoord coord,
        MapPoint point,
        out CombatRoom combatRoom)
    {
        combatRoom = null!;

        var historyEntry = runState.GetHistoryEntryFor(new MapLocation(coord, runState.CurrentActIndex));
        var expectedRoomType = point.PointType switch
        {
            MapPointType.Elite => RoomType.Elite,
            MapPointType.Boss => RoomType.Boss,
            _ => RoomType.Monster
        };

        var roomHistory = historyEntry?.Rooms.FirstOrDefault(room => room.RoomType == expectedRoomType)
                          ?? historyEntry?.Rooms.FirstOrDefault(room =>
                              room.RoomType is RoomType.Monster or RoomType.Elite or RoomType.Boss);
        if (roomHistory == null || roomHistory.ModelId == ModelId.none)
        {
            MainFile.Logger.Warn(
                $"[DreamMode] Failed to rebuild pre-finished combat room for coord=({coord.col},{coord.row}) because no combat history entry was found.");
            return false;
        }

        var canonicalEncounter = ModelDb.GetById<EncounterModel>(roomHistory.ModelId);
        if (canonicalEncounter == null)
        {
            MainFile.Logger.Warn(
                $"[DreamMode] Failed to rebuild pre-finished combat room for coord=({coord.col},{coord.row}) because encounter '{roomHistory.ModelId}' was missing.");
            return false;
        }

        var mutableEncounter = (canonicalEncounter.CanonicalInstance ?? canonicalEncounter).ToMutable();
        EncounterShouldGiveRewardsField?.SetValue(mutableEncounter, false);

        combatRoom = new CombatRoom(mutableEncounter, runState);
        CombatRoomMarkPreFinishedMethod?.Invoke(combatRoom, []);
        MainFile.Logger.Info(
            $"[DreamMode] Rebuilt pre-finished combat room | coord=({coord.col},{coord.row}) | expectedRoomType={expectedRoomType} | encounter={roomHistory.ModelId}.");
        return true;
    }

    private static Task InvokeLoadIntoLatestMapCoordAsync(
        RunManager runManager,
        AbstractRoom? preFinishedRoom)
    {
        if (LoadIntoLatestMapCoordMethod?.Invoke(runManager, [preFinishedRoom]) is Task task)
            return task;

        return Task.CompletedTask;
    }

    private static Task InvokeEnterMapCoordInternalAsync(
        RunManager runManager,
        MapCoord coord,
        AbstractRoom? preFinishedRoom,
        bool saveGame)
    {
        if (EnterMapCoordInternalMethod?.Invoke(runManager, [coord, preFinishedRoom, saveGame]) is Task task)
            return task;

        return Task.CompletedTask;
    }
}
