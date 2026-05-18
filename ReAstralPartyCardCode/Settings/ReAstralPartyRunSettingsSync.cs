using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public sealed class ReAstralPartyRunSettingsSnapshot
{
    public bool EnableAllPersonas { get; set; }

    public bool EnableDuplicatePersonas { get; set; }

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;
}

internal static class ReAstralPartyRunSettingsSync
{
    private const int SnapshotChoiceMagic = unchecked((int)0x52415353);
    private const int SnapshotChoiceKind = 1;
    private const int SnapshotSchemaVersion = 1;

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

                MainFile.Logger.Warn(
                    $"{MainFile.ModId} background settings sync failed: {completedTask.Exception.GetBaseException().Message}");
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public static bool TryGetSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (runState is not RunState concreteRunState)
            return false;

        if (!RunStates.TryGetValue(concreteRunState, out var state) || state.Snapshot == null)
            return false;

        snapshot = state.Snapshot;
        return true;
    }

    private static ReAstralPartyRunSettingsSnapshot CreateLocalSnapshot()
    {
        var settings = ReAstralPartyModSettingsManager.ReadLocalSettings();
        return new ReAstralPartyRunSettingsSnapshot
        {
            EnableAllPersonas = settings.EnableAllPersonas,
            EnableDuplicatePersonas = settings.EnableDuplicatePersonas,
            TokenSeriesMode = ReAstralPartyModSettingsManager.ResolveTokenSeriesMode(settings),
            EnablePureAngelMode = settings.EnablePureAngelMode
        };
    }

    private static ReAstralPartyRunSettingsSnapshot CreateSafeSnapshot()
    {
        return new ReAstralPartyRunSettingsSnapshot
        {
            EnableAllPersonas = false,
            EnableDuplicatePersonas = false,
            TokenSeriesMode = TokenSeriesMode.RandomTwo,
            EnablePureAngelMode = false
        };
    }

    private static PlayerChoiceResult CreateSnapshotChoiceResult(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        return PlayerChoiceResult.FromIndexes([
            SnapshotChoiceMagic,
            SnapshotChoiceKind,
            SnapshotSchemaVersion,
            snapshot.EnableAllPersonas ? 1 : 0,
            snapshot.EnableDuplicatePersonas ? 1 : 0,
            (int)snapshot.TokenSeriesMode,
            snapshot.EnablePureAngelMode ? 1 : 0
        ]);
    }

    private static bool TryDecodeSnapshotChoiceResult(PlayerChoiceResult result,
        out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;

        try
        {
            var payload = result.AsIndexes();
            if (payload == null || payload.Count < 7)
                return false;
            if (payload[0] != SnapshotChoiceMagic || payload[1] != SnapshotChoiceKind ||
                payload[2] != SnapshotSchemaVersion)
                return false;

            snapshot = new ReAstralPartyRunSettingsSnapshot
            {
                EnableAllPersonas = payload[3] != 0,
                EnableDuplicatePersonas = payload[4] != 0,
                TokenSeriesMode = Enum.IsDefined(typeof(TokenSeriesMode), payload[5])
                    ? (TokenSeriesMode)payload[5]
                    : TokenSeriesMode.RandomTwo,
                EnablePureAngelMode = payload[6] != 0
            };
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static async Task<ReAstralPartyRunSettingsSnapshot> SyncAsync(RunState runState, RunSettingsSyncState state)
    {
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(localSnapshot);
            return localSnapshot;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because PlayerChoiceSynchronizer was unavailable; using safe multiplayer defaults.");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var authorityNetId = ResolveAuthorityNetId(netService);
        if (authorityNetId == 0UL)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because authority net id was unavailable; using safe multiplayer defaults.");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var authorityPlayer = runState.Players.FirstOrDefault(player => player.NetId == authorityNetId);
        if (authorityPlayer == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because authority player {authorityNetId} was unavailable; using safe multiplayer defaults.");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var isHost = netService.Type == NetGameType.Host;
        if (!isHost)
        {
            var remoteChoiceId = synchronizer.ReserveChoiceId(authorityPlayer);
            var remoteChoice = await synchronizer.WaitForRemoteChoice(authorityPlayer, remoteChoiceId);
            if (TryDecodeSnapshotChoiceResult(remoteChoice, out var remoteSnapshot))
            {
                state.SetSnapshot(remoteSnapshot);
                MainFile.Logger.Info(
                    $"{MainFile.ModId} settings sync received from host player {authorityPlayer.NetId}: all_personas={remoteSnapshot.EnableAllPersonas}, duplicate_personas={remoteSnapshot.EnableDuplicatePersonas}, token_series={remoteSnapshot.TokenSeriesMode}, pure_angel={remoteSnapshot.EnablePureAngelMode}");
                return remoteSnapshot;
            }
        }

        var choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
        MainFile.Logger.Info(
            $"{MainFile.ModId} settings sync resolved netMode={netService.Type}, host={isHost}, authority={authorityPlayer?.NetId.ToString() ?? "<null>"}.");

        if (isHost && authorityPlayer != null)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(localSnapshot);
            synchronizer.SyncLocalChoice(authorityPlayer, choiceId, CreateSnapshotChoiceResult(localSnapshot));
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync broadcast by authority player {authorityPlayer.NetId}: all_personas={localSnapshot.EnableAllPersonas}, duplicate_personas={localSnapshot.EnableDuplicatePersonas}, token_series={localSnapshot.TokenSeriesMode}, pure_angel={localSnapshot.EnablePureAngelMode}");
            return localSnapshot;
        }

        var fallbackSnapshot = CreateSafeSnapshot();
        MainFile.Logger.Warn(
            $"{MainFile.ModId} settings sync did not have a host authority path; using safe multiplayer defaults.");
        state.SetSnapshot(fallbackSnapshot);
        return fallbackSnapshot;
    }

    private static ulong ResolveAuthorityNetId(INetGameService netService)
    {
        if (netService.Type == NetGameType.Host)
            return netService.NetId;

        if (netService is INetClientGameService clientService)
            return clientService.NetClient.HostNetId;

        return 0UL;
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        const int maxSynchronizerWaitFrames = 600;
        for (var i = 0; i < maxSynchronizerWaitFrames; i++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
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

        public void SetSnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
        {
            lock (_gate)
            {
                Snapshot = snapshot;
            }
        }
    }
}
