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
    public bool EnableExtremeMode { get; set; }

    public bool EnableAllPersonas { get; set; }

    public bool EnableAllVariantPersonas { get; set; }

    public StartingPersonaMode StartingPersonaMode { get; set; } = StartingPersonaMode.Standard;

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;

    public List<string> BannedRelicIdsSerialized { get; set; } = [];

    public List<string> BannedPersonaRelicIdsSerialized
    {
        get => BannedRelicIdsSerialized;
        set => BannedRelicIdsSerialized = value;
    }
}

internal static class ReAstralPartyRunSettingsSync
{
    private static readonly AttachedState<RunState, RunSettingsSyncState> RunStates = new(() =>
        new RunSettingsSyncState());
    private const int MaxClientSnapshotAttempts = 64;

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
            EnableExtremeMode = settings.EnableExtremeMode,
            EnableAllPersonas = settings.EnableAllPersonas,
            EnableAllVariantPersonas = settings.EnableAllVariantPersonas,
            StartingPersonaMode = ReAstralPartyModSettingsManager.ResolveStartingPersonaMode(settings),
            TokenSeriesMode = ReAstralPartyModSettingsManager.ResolveTokenSeriesMode(settings),
            EnablePureAngelMode = settings.EnablePureAngelMode,
            BannedRelicIdsSerialized = [.. (settings.BannedRelicIds.Count > 0
                ? settings.BannedRelicIds
                : settings.BannedPersonaRelicIds ?? [])
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)]
        };
    }

    private static ReAstralPartyRunSettingsSnapshot CreateSafeSnapshot()
    {
        return new ReAstralPartyRunSettingsSnapshot
        {
            EnableExtremeMode = false,
            EnableAllPersonas = false,
            EnableAllVariantPersonas = false,
            StartingPersonaMode = StartingPersonaMode.Standard,
            TokenSeriesMode = TokenSeriesMode.RandomTwo,
            EnablePureAngelMode = false,
            BannedRelicIdsSerialized = []
        };
    }

    private static PlayerChoiceResult CreateSnapshotChoiceResult(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        var personaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics();
        var bannedSet = snapshot.BannedRelicIdsSerialized
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var bannedFlags = personaRelics
            .Select(relic => bannedSet.Contains((relic.CanonicalInstance?.Id ?? relic.Id).ToString()) ? 1 : 0);

        return PlayerChoiceResult.FromIndexes([
            snapshot.EnableExtremeMode ? 1 : 0,
            snapshot.EnableAllPersonas ? 1 : 0,
            snapshot.EnableAllVariantPersonas ? 1 : 0,
            (int)snapshot.StartingPersonaMode,
            (int)snapshot.TokenSeriesMode,
            snapshot.EnablePureAngelMode ? 1 : 0,
            .. bannedFlags
        ]);
    }

    private static bool TryDecodeSnapshotChoiceResult(PlayerChoiceResult result,
        out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (!TryGetIndexPayload(result, out var payload) || payload.Count < 5)
            return false;

        var personaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics();
        var isLegacyPayload = payload.Count == personaRelics.Count + 7;
        var payloadOffset = isLegacyPayload ? 1 : 0;
        var bannedStartIndex = payloadOffset + (isLegacyPayload ? 6 : 5);
        var bannedIds = new List<string>();
        for (var i = 0; i < personaRelics.Count && bannedStartIndex + i < payload.Count; i++)
        {
            if (payload[bannedStartIndex + i] == 0)
                continue;

            bannedIds.Add((personaRelics[i].CanonicalInstance?.Id ?? personaRelics[i].Id).ToString());
        }

        var legacyRandomCloneMode = isLegacyPayload && payload[payloadOffset + 3] != 0;
        snapshot = new ReAstralPartyRunSettingsSnapshot
        {
            EnableExtremeMode = payloadOffset > 0 && payload[0] != 0,
            EnableAllPersonas = payload[payloadOffset] != 0,
            EnableAllVariantPersonas = payload[payloadOffset + 1] != 0,
            StartingPersonaMode = isLegacyPayload
                ? StartingPersonaMode.Standard
                : Enum.IsDefined(typeof(StartingPersonaMode), payload[payloadOffset + 2])
                    ? (StartingPersonaMode)payload[payloadOffset + 2]
                    : StartingPersonaMode.Standard,
            TokenSeriesMode = Enum.IsDefined(typeof(TokenSeriesMode), payload[payloadOffset + (isLegacyPayload ? 4 : 3)])
                ? (TokenSeriesMode)payload[payloadOffset + (isLegacyPayload ? 4 : 3)]
                : TokenSeriesMode.RandomTwo,
            EnablePureAngelMode = payload[payloadOffset + (isLegacyPayload ? 5 : 4)] != 0,
            BannedRelicIdsSerialized = bannedIds
        };
        if (isLegacyPayload)
        {
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync decoded legacy starting persona payload and downgraded it to standard mode (legacy_random_clone_mode={legacyRandomCloneMode}).");
        }
        return true;
    }

    private static async Task<ReAstralPartyRunSettingsSnapshot> SyncAsync(RunState runState, RunSettingsSyncState state)
    {
        var runManager = RunManager.Instance;
        if (runManager == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because RunManager was unavailable; using safe multiplayer defaults.");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var netService = runManager.NetService;
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
            for (var attempt = 0; attempt < MaxClientSnapshotAttempts; attempt++)
            {
                var remoteChoiceId = synchronizer.ReserveChoiceId(authorityPlayer);
                var remoteChoice = await synchronizer.WaitForRemoteChoice(authorityPlayer, remoteChoiceId);
                if (!TryDecodeSnapshotChoiceResult(remoteChoice, out var remoteSnapshot))
                {
                    MainFile.Logger.Warn(
                        $"{MainFile.ModId} settings sync ignored foreign choice from authority player {authorityPlayer.NetId}: choiceId={remoteChoiceId} attempt={attempt + 1} result={remoteChoice}");
                    continue;
                }

                state.SetSnapshot(remoteSnapshot);
                MainFile.Logger.Info(
                    $"{MainFile.ModId} settings sync received from host player {authorityPlayer.NetId}: extreme_mode={remoteSnapshot.EnableExtremeMode}, all_personas={remoteSnapshot.EnableAllPersonas}, all_variants={remoteSnapshot.EnableAllVariantPersonas}, persona_mode={remoteSnapshot.StartingPersonaMode}, token_series={remoteSnapshot.TokenSeriesMode}, pure_angel={remoteSnapshot.EnablePureAngelMode}, banned_relics={remoteSnapshot.BannedRelicIdsSerialized.Count}");
                return remoteSnapshot;
            }

            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync did not receive a valid host snapshot after {MaxClientSnapshotAttempts} attempts; falling back to safe multiplayer defaults.");
        }

        var choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
        MainFile.Logger.Info(
            $"{MainFile.ModId} settings sync resolved netMode={netService.Type}, host={isHost}, authority={authorityPlayer.NetId}.");

        if (isHost)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(localSnapshot);
            synchronizer.SyncLocalChoice(authorityPlayer, choiceId, CreateSnapshotChoiceResult(localSnapshot));
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync broadcast by authority player {authorityPlayer.NetId}: extreme_mode={localSnapshot.EnableExtremeMode}, all_personas={localSnapshot.EnableAllPersonas}, all_variants={localSnapshot.EnableAllVariantPersonas}, persona_mode={localSnapshot.StartingPersonaMode}, token_series={localSnapshot.TokenSeriesMode}, pure_angel={localSnapshot.EnablePureAngelMode}, banned_relics={localSnapshot.BannedRelicIdsSerialized.Count}");
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
        {
            var netClient = clientService.NetClient;
            return netClient?.HostNetId ?? 0UL;
        }

        return 0UL;
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        ArgumentNullException.ThrowIfNull(runManager);
        const int maxSynchronizerWaitFrames = 600;
        for (var i = 0; i < maxSynchronizerWaitFrames; i++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
    }

    private static bool TryGetIndexPayload(PlayerChoiceResult result, out List<int> payload)
    {
        payload = [];
        try
        {
            var indexes = result.AsIndexes();
            if (indexes == null)
                return false;

            payload = indexes;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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
