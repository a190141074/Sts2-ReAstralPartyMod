using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public sealed class ReAstralPartyRunSettingsSnapshot
{
    public AstralContentMode CurrentContentMode { get; set; } = AstralContentMode.Vanilla;

    public bool EnableExtremeMode { get; set; }

    public bool EnableStartingInitialPoint { get; set; }

    public bool EnableStartingAstralRelicStore { get; set; } = true;

    public bool EnableStartingPersonSelection { get; set; } = true;

    public bool EnableJewelryRelics { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;


    public bool EnableCollectorsCards { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersons { get; set; }

    public bool EnableVariantPersons { get; set; } = true;

    public bool EnableAllVariantPersons { get; set; }

    public StartingPersonMode StartingPersonMode { get; set; } = StartingPersonMode.Standard;

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;
















    public List<string> BannedRelicIdsSerialized { get; set; } = [];

    public List<string> BannedPersonRelicIdsSerialized
    {
        get => BannedRelicIdsSerialized;
        set => BannedRelicIdsSerialized = value;
    }

    public bool EnableStartingPersonaSelection
    {
        get => EnableStartingPersonSelection;
        set => EnableStartingPersonSelection = value;
    }

    public bool EnableAllPersonas
    {
        get => EnableAllPersons;
        set => EnableAllPersons = value;
    }

    public bool EnableVariantPersonas
    {
        get => EnableVariantPersons;
        set => EnableVariantPersons = value;
    }

    public bool EnableAllVariantPersonas
    {
        get => EnableAllVariantPersons;
        set => EnableAllVariantPersons = value;
    }

    public StartingPersonMode StartingPersonaMode
    {
        get => StartingPersonMode;
        set => StartingPersonMode = value;
    }

    public List<string> BannedPersonaRelicIdsSerialized
    {
        get => BannedPersonRelicIdsSerialized;
        set => BannedPersonRelicIdsSerialized = value;
    }

    public ReAstralPartyRunSettingsSnapshot Clone()
    {
        return new ReAstralPartyRunSettingsSnapshot
        {
            CurrentContentMode = CurrentContentMode,
            EnableExtremeMode = EnableExtremeMode,
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableJewelryRelics = EnableJewelryRelics,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            StartingPersonMode = StartingPersonMode,
            TokenSeriesMode = TokenSeriesMode,
            EnablePureAngelMode = EnablePureAngelMode,
            BannedRelicIdsSerialized = [.. BannedRelicIdsSerialized]
        };
    }
}

internal static class ReAstralPartyRunSettingsSync
{
    private static readonly AttachedState<RunState, RunSettingsSyncState> RunStates = new(() =>
        new RunSettingsSyncState());

    public static Task EnsureSyncedAsync(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);
        return RunStates.GetOrCreate(runState).EnsureSyncedAsync(runState);
    }

    public static void BeginSyncIfNeeded(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);

        var task = RunStates.GetOrCreate(runState).EnsureSyncedAsync(runState);
        if (task.IsCompleted)
            return;

        _ = task.ContinueWith(
            completedTask =>
            {
                if (completedTask.Exception == null)
                    return;

                var message = completedTask.Exception.GetBaseException().Message;
                MainFile.Logger.Warn(
                    $"{MainFile.ModId} background settings sync failed: {message}");
                ShowSyncError(
                    210,
                    "后台同步",
                    $"联机玩法设置后台同步失败，当前局可能会退回安全默认值。\n原因：{message}");
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public static bool TryGetSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (runState is not RunState concreteRunState)
            return false;

        if (ReAstralPartyModSettingsManager.TryGetStoredRunSettingsSnapshot(concreteRunState, out snapshot))
            return true;

        if (!RunStates.TryGetValue(concreteRunState, out var state) || state.Snapshot == null)
            return false;

        snapshot = state.Snapshot.Clone();
        return true;
    }

    private static ReAstralPartyRunSettingsSnapshot CreateLocalSnapshot()
    {
        var snapshot = CreateRuntimeSnapshot();
        LobbyGameplaySettingsSync.TryGetSnapshot(out var lobbySnapshot);
        if (lobbySnapshot == null)
            return snapshot;

        snapshot.CurrentContentMode = lobbySnapshot.CurrentContentMode;
        snapshot.EnableStartingInitialPoint = lobbySnapshot.EnableStartingInitialPoint;
        snapshot.EnableStartingAstralRelicStore = lobbySnapshot.EnableStartingAstralRelicStore;
        snapshot.EnableStartingPersonaSelection = lobbySnapshot.EnableStartingPersonaSelection;
        snapshot.EnableJewelryRelics = lobbySnapshot.EnableJewelryRelics;
        snapshot.EnableNeowExtraOption = lobbySnapshot.EnableNeowExtraOption;
        snapshot.EnableCollectorsCards = lobbySnapshot.EnableCollectorsCards;
        snapshot.NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
            lobbySnapshot.NeowExtraOptionSelectionMode);
        snapshot.EnableAllPersonas = lobbySnapshot.EnableAllPersonas;
        snapshot.EnableVariantPersons = lobbySnapshot.EnableVariantPersons;
        snapshot.EnableAllVariantPersons = lobbySnapshot.EnableAllVariantPersons;
        snapshot.StartingPersonMode = lobbySnapshot.StartingPersonMode;
        snapshot.TokenSeriesMode = lobbySnapshot.TokenSeriesMode;
        snapshot.EnableExtremeMode = lobbySnapshot.EnableExtremeMode;
        snapshot.BannedRelicIdsSerialized = [.. ReAstralPartyModSettingsManager.SanitizeSerializedBannedRelicIds(
            lobbySnapshot.BannedRelicIdsSerialized)]
        ;
        return snapshot;
    }

    private static ReAstralPartyRunSettingsSnapshot CreateSafeSnapshot()
    {
        return CreateRuntimeSnapshot();
    }

    private static ReAstralPartyRunSettingsSnapshot CreateRuntimeSnapshot()
    {
        return new ReAstralPartyRunSettingsSnapshot
        {
            CurrentContentMode = ReAstralPartyModSettingsManager.GetCurrentContentMode(),
            EnableExtremeMode = ReAstralPartyModSettingsManager.EnableExtremeMode,
            EnableStartingInitialPoint = ReAstralPartyModSettingsManager.EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = ReAstralPartyModSettingsManager.EnableStartingAstralRelicStore,
            EnableStartingPersonaSelection = ReAstralPartyModSettingsManager.EnableStartingPersonaSelection,
            EnableJewelryRelics = ReAstralPartyModSettingsManager.EnableJewelryRelics,
            EnableNeowExtraOption = ReAstralPartyModSettingsManager.EnableNeowExtraOption,
            EnableCollectorsCards = ReAstralPartyModSettingsManager.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                ReAstralPartyModSettingsManager.NeowExtraOptionSelectionMode),
            EnableAllPersonas = ReAstralPartyModSettingsManager.EnableAllPersonas,
            EnableVariantPersons = ReAstralPartyModSettingsManager.EnableVariantPersons,
            EnableAllVariantPersons = ReAstralPartyModSettingsManager.EnableAllVariantPersons,
            StartingPersonMode = ReAstralPartyModSettingsManager.ConfiguredStartingPersonaMode,
            TokenSeriesMode = ReAstralPartyModSettingsManager.TokenSeriesMode,
            EnablePureAngelMode = ReAstralPartyModSettingsManager.EnablePureAngelMode,
            BannedRelicIdsSerialized = [.. ReAstralPartyModSettingsManager.GetBannedRelicIds(null)
                .Where(static id => id != MegaCrit.Sts2.Core.Models.ModelId.none)
                .Select(static id => id.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)]
        };
    }

    private static Task<ReAstralPartyRunSettingsSnapshot> SyncAsync(RunState runState, RunSettingsSyncState state)
    {
        if (ReAstralPartyModSettingsManager.TryGetStoredRunSettingsSnapshot(runState, out var storedSnapshot))
        {
            state.SetSnapshot(runState, storedSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            LogSnapshotEstablished(runState, "stored_snapshot");
            return Task.FromResult(storedSnapshot);
        }

        var runManager = RunManager.Instance;
        if (runManager == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because RunManager was unavailable; using safe multiplayer defaults.");
            ShowSyncWarning(
                213,
                "同步前置",
                "联机玩法设置同步时未拿到 RunManager，已退回安全默认值。");
            state.SetSnapshot(runState, safeSnapshot);
            return Task.FromResult(safeSnapshot);
        }

        var netService = runManager.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(runState, localSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            LogSnapshotEstablished(runState, "local_singleplayer");
            return Task.FromResult(localSnapshot);
        }

        if (TryCreateRunSnapshotFromLobby(out var lobbySnapshot))
        {
            state.SetSnapshot(runState, lobbySnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            LogSnapshotEstablished(runState, "lobby_snapshot");
            MainFile.Logger.Info($"{MainFile.ModId} settings sync established from lobby snapshot.");
            return Task.FromResult(lobbySnapshot);
        }

        if (netService.Type == NetGameType.Host)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(runState, localSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            LogSnapshotEstablished(runState, "host_runtime_fallback");
            MainFile.Logger.Info($"{MainFile.ModId} settings sync established from host runtime fallback.");
            return Task.FromResult(localSnapshot);
        }

        var fallbackSnapshot = CreateSafeSnapshot();
        MainFile.Logger.Warn(
            $"{MainFile.ModId} settings sync did not find an established lobby snapshot on the client before run start; using safe multiplayer defaults.");
        ShowSyncWarning(
            218,
            "主机路径",
            "客户端在开局时未拿到房间玩法设置快照，已退回安全默认值。请检查房间设置同步是否在开局前完成。");
        state.SetSnapshot(runState, fallbackSnapshot);
        return Task.FromResult(fallbackSnapshot);
    }

    private static bool TryCreateRunSnapshotFromLobby(out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (!LobbyGameplaySettingsSync.TryGetSnapshot(out var lobbySnapshot))
            return false;

        var settings = ReAstralPartyModSettingsManager.ReadLocalSettings();
        snapshot = new ReAstralPartyRunSettingsSnapshot
        {
            CurrentContentMode = lobbySnapshot.CurrentContentMode,
            EnableExtremeMode = lobbySnapshot.EnableExtremeMode,
            EnableStartingInitialPoint = lobbySnapshot.EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = lobbySnapshot.EnableStartingAstralRelicStore,
            EnableStartingPersonaSelection = lobbySnapshot.EnableStartingPersonaSelection,
            EnableJewelryRelics = lobbySnapshot.EnableJewelryRelics,
            EnableNeowExtraOption = lobbySnapshot.EnableNeowExtraOption,
            EnableCollectorsCards = lobbySnapshot.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                lobbySnapshot.NeowExtraOptionSelectionMode),
            EnableAllPersonas = lobbySnapshot.EnableAllPersonas,
            EnableVariantPersonas = lobbySnapshot.EnableVariantPersonas,
            EnableAllVariantPersonas = lobbySnapshot.EnableAllVariantPersonas,
            StartingPersonMode = lobbySnapshot.StartingPersonMode,
            TokenSeriesMode = lobbySnapshot.TokenSeriesMode,
            EnablePureAngelMode = settings.EnablePureAngelMode,
            BannedRelicIdsSerialized =
            [
                .. ReAstralPartyModSettingsManager.SanitizeSerializedBannedRelicIds(
                    settings.BannedRelicIds.Count > 0
                        ? settings.BannedRelicIds
                        : settings.BannedPersonaRelicIds)
            ]
        };
        return true;
    }

    private static void LogSnapshotEstablished(RunState runState, string source)
    {
        var netMode = RunManager.Instance?.NetService?.Type.ToString() ?? "Unknown";
        var runKey = StartingPersonRelicSelectionPatch.GetRunKey(runState);
        var personaWindow = StartingPersonRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out var reason);
        MainFile.Logger.Info(
            $"{MainFile.ModId} settings sync established before persona ready flow: source={source}, runKey={runKey}, netMode={netMode}, persona_window_open={personaWindow}, persona_gate_reason={reason}");
    }

    private static void ShowSyncWarning(int number, string stage, string body)
    {
        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.Multiplayer,
            number,
            body,
            stage);
    }

    private static void ShowSyncError(int number, string stage, string body)
    {
        AstralNotificationService.ShowDiagnosticError(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.Multiplayer,
            number,
            body,
            stage);
    }

    private sealed class RunSettingsSyncState
    {
        private readonly object _gate = new();
        private Task<ReAstralPartyRunSettingsSnapshot>? _syncTask;

        public ReAstralPartyRunSettingsSnapshot? Snapshot { get; private set; }

        public Task<ReAstralPartyRunSettingsSnapshot> EnsureSyncedAsync(RunState runState)
        {
            lock (_gate)
            {
                if (_syncTask != null)
                    return _syncTask;

                _syncTask = SyncAsync(runState, this);
                return _syncTask;
            }
        }

        public void SetSnapshot(RunState runState, ReAstralPartyRunSettingsSnapshot snapshot)
        {
            lock (_gate)
            {
                Snapshot = snapshot.Clone();
            }

            ReAstralPartyModSettingsManager.StoreRunSettingsSnapshot(runState, snapshot);
        }
    }
}


