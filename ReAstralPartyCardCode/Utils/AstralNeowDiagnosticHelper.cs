using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralNeowDiagnosticHelper
{
    private const string RefreshAncientModId = "RefreshAncient";

    private static readonly object ToastLock = new();
    private static readonly HashSet<string> ShownToastKeys = [];

    public static void ReportPostPersonaSelectionWindow(IRunState? runState, int optionCount)
    {
        var snapshot = BuildSnapshot(runState);
        MainFile.Logger.Warn(
            $"[M201] Post-persona NEOW window snapshot | options={optionCount} | {FormatSnapshotForLog(snapshot)}");
    }

    public static void ReportEventRoomNodeReady(object? roomNode)
    {
        var snapshot = BuildSnapshot(RunManager.Instance?.DebugOnlyGetState());
        var nodeType = roomNode?.GetType().FullName ?? "null";
        MainFile.Logger.Warn(
            $"[M202] Event room node ready | node={nodeType} | {FormatSnapshotForLog(snapshot)}");
    }

    public static void ReportAncientLayoutReady(object? layoutNode)
    {
        var snapshot = BuildSnapshot(RunManager.Instance?.DebugOnlyGetState());
        var nodeType = layoutNode?.GetType().FullName ?? "null";
        var optionCount = TryCountRelicLikeEntries(layoutNode);
        MainFile.Logger.Warn(
            $"[M203] Ancient layout ready | node={nodeType} | optionCount={optionCount} | {FormatSnapshotForLog(snapshot)}");
    }

    public static void ReportGrabBagRaritySnapshot(object? grabBag, Player? player)
    {
        if (player?.RunState == null || grabBag == null)
            return;

        var relics = EnumerateRelicsFromObject(grabBag)
            .DistinctBy(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToList();
        if (relics.Count == 0)
            return;

        var unusualGroups = relics
            .GroupBy(static relic => relic.Rarity)
            .Where(static group => group.Key == RelicRarity.Ancient)
            .OrderBy(static group => group.Key.ToString(), StringComparer.Ordinal)
            .ToList();
        if (unusualGroups.Count == 0)
            return;

        var snapshot = BuildSnapshot(player.RunState);
        if (!snapshot.IsEventLike && !snapshot.RefreshAncientLoaded)
            return;

        var raritySummary = string.Join(
            " | ",
            unusualGroups.Select(group =>
                $"{group.Key}:{string.Join(",", group.Take(4).Select(relic => (relic.CanonicalInstance ?? relic).Id.Entry))}"));

        MainFile.Logger.Warn(
            $"[M204] Ancient relic rarity found in grab bag during NEOW diagnostics window | owner={player.NetId} | {raritySummary} | {FormatSnapshotForLog(snapshot)}");

        var toastKey = $"M204:{snapshot.RunKey}:{player.NetId}:{raritySummary}";
        if (!TryAcquireToast(toastKey))
            return;

        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.NeowDiagnostics,
            204,
            $"发现 Ancient 稀有度遗物进入随机遗物袋。\n玩家：{player.NetId}\n{raritySummary}\n{FormatSnapshotForBody(snapshot)}",
            "Ancient混入");
    }

    private static Snapshot BuildSnapshot(IRunState? runState)
    {
        var players = runState?.Players
            .Select(static player => player.NetId.ToString())
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray() ?? [];
        var room = runState?.CurrentRoom;
        var roomType = room?.GetType().FullName ?? "no_room";
        var roomKind = ReadSimpleProperty(room, "RoomType");
        var eventId = TryReadEventId(room);
        var floor = ReadSimpleProperty(runState, "CurrentFloor")
                    ?? ReadSimpleProperty(runState, "Floor")
                    ?? "unknown_floor";
        var runKey = runState == null
            ? "no_run"
            : $"{runState.Rng.StringSeed}|{string.Join(",", players)}|act:{runState.CurrentActIndex}|floor:{floor}|room:{roomType}";
        var refreshAncientLoaded = OptionalModCompatRegistry.IsModLoaded(RefreshAncientModId);

        return new Snapshot(
            runKey,
            runState?.Rng.StringSeed ?? "no_seed",
            runState?.CurrentActIndex ?? -1,
            floor,
            roomType,
            roomKind ?? "unknown_room_kind",
            eventId ?? "unknown_event",
            players,
            refreshAncientLoaded);
    }

    private static string FormatSnapshotForLog(Snapshot snapshot)
    {
        return $"seed={snapshot.Seed} act={snapshot.ActIndex} floor={snapshot.Floor} room={snapshot.RoomType} roomKind={snapshot.RoomKind} event={snapshot.EventId} players={string.Join(",", snapshot.PlayerNetIds)} refreshAncient={snapshot.RefreshAncientLoaded}";
    }

    private static string FormatSnapshotForBody(Snapshot snapshot)
    {
        return $"Seed：{snapshot.Seed}\nAct：{snapshot.ActIndex}\nFloor：{snapshot.Floor}\n房间：{snapshot.RoomType}\nRoomType：{snapshot.RoomKind}\n事件：{snapshot.EventId}\n玩家：{string.Join(", ", snapshot.PlayerNetIds)}\nRefreshAncient：{snapshot.RefreshAncientLoaded}";
    }

    private static bool TryAcquireToast(string key)
    {
        lock (ToastLock)
        {
            return ShownToastKeys.Add(key);
        }
    }

    private static string? TryReadEventId(object? room)
    {
        foreach (var memberName in new[] { "Event", "CurrentEvent", "_event", "eventModel" })
        {
            var value = ReadMemberValue(room, memberName);
            if (value == null)
                continue;

            var idValue = ReadMemberValue(value, "Id");
            var entry = ReadMemberValue(idValue, "Entry")?.ToString();
            if (!string.IsNullOrWhiteSpace(entry))
                return entry;

            var asString = value.ToString();
            if (!string.IsNullOrWhiteSpace(asString))
                return asString;
        }

        return null;
    }

    private static string? ReadSimpleProperty(object? instance, string memberName)
    {
        return ReadMemberValue(instance, memberName)?.ToString();
    }

    private static object? ReadMemberValue(object? instance, string memberName)
    {
        if (instance == null)
            return null;

        try
        {
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var property = instance.GetType().GetProperty(memberName, flags);
            if (property != null)
                return property.GetValue(instance);

            var field = instance.GetType().GetField(memberName, flags);
            return field?.GetValue(instance);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[AstralNeowDiagnostics] Failed to read member '{memberName}' from {instance.GetType().FullName}: {ex.Message}");
            return null;
        }
    }

    private static int TryCountRelicLikeEntries(object? instance)
    {
        if (instance == null)
            return -1;

        foreach (var memberName in new[] { "Options", "_options", "Buttons", "_buttons", "Entries", "_entries" })
        {
            var value = ReadMemberValue(instance, memberName);
            if (value is ICollection collection)
                return collection.Count;
        }

        return -1;
    }

    private static IEnumerable<RelicModel> EnumerateRelicsFromObject(object instance)
    {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        foreach (var field in instance.GetType().GetFields(flags))
        {
            foreach (var relic in EnumerateRelicsFromValue(field.GetValue(instance)))
                yield return relic;
        }

        foreach (var property in instance.GetType().GetProperties(flags))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            object? value;
            try
            {
                value = property.GetValue(instance);
            }
            catch
            {
                continue;
            }

            foreach (var relic in EnumerateRelicsFromValue(value))
                yield return relic;
        }
    }

    private static IEnumerable<RelicModel> EnumerateRelicsFromValue(object? value)
    {
        if (value == null)
            yield break;

        if (value is RelicModel relic)
        {
            yield return relic;
            yield break;
        }

        if (value is string || value is not IEnumerable enumerable)
            yield break;

        foreach (var entry in enumerable)
        {
            if (entry is RelicModel nestedRelic)
                yield return nestedRelic;
        }
    }

    private sealed record Snapshot(
        string RunKey,
        string Seed,
        int ActIndex,
        string Floor,
        string RoomType,
        string RoomKind,
        string EventId,
        IReadOnlyList<string> PlayerNetIds,
        bool RefreshAncientLoaded)
    {
        public bool IsEventLike =>
            RoomType.Contains("Event", StringComparison.OrdinalIgnoreCase)
            || RoomKind.Contains("Event", StringComparison.OrdinalIgnoreCase)
            || EventId.Contains("NEOW", StringComparison.OrdinalIgnoreCase)
            || EventId.Contains("ANCIENT", StringComparison.OrdinalIgnoreCase);
    }
}
