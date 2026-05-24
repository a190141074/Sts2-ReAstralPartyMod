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
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
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
    private const string SnapshotSessionKey = "run_settings_snapshot";

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

        if (!RunStates.TryGetValue(concreteRunState, out var state) || state.Snapshot == null)
            return false;

        snapshot = state.Snapshot;
        return true;
    }

    private static ReAstralPartyRunSettingsSnapshot CreateLocalSnapshot()
    {
        var settings = ReAstralPartyModSettingsManager.ReadLocalSettings();
        LobbyGameplaySettingsSync.TryGetSnapshot(out var lobbySnapshot);
        return new ReAstralPartyRunSettingsSnapshot
        {
            EnableAllPersonas = lobbySnapshot?.EnableAllPersonas ?? settings.EnableAllPersonas,
            EnableAllVariantPersonas = lobbySnapshot?.EnableAllVariantPersonas ?? settings.EnableAllVariantPersonas,
            StartingPersonaMode = lobbySnapshot?.StartingPersonaMode
                                  ?? ReAstralPartyModSettingsManager.ResolveStartingPersonaMode(settings),
            TokenSeriesMode = lobbySnapshot?.TokenSeriesMode
                              ?? ReAstralPartyModSettingsManager.ResolveTokenSeriesMode(settings),
            EnableExtremeMode = lobbySnapshot?.EnableExtremeMode ?? settings.EnableExtremeMode,
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

    private static PlayerChoiceResult CreateSnapshotChoiceResult(RunState runState,
        ReAstralPartyRunSettingsSnapshot snapshot)
    {
        var payload = CreateSnapshotPayload(snapshot);
        return AstralChoiceProtocol.CreateIndexedEnvelope(
            AstralChoiceKind.RunSettingsSnapshot,
            runState,
            SnapshotSessionKey,
            sequence: 0,
            payload);
    }

    private static List<int> CreateSnapshotPayload(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        var bannableRelics = BannedRelicRegistry.GetCanonicalBannableRelics();
        var bannedSet = snapshot.BannedRelicIdsSerialized
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
        var bannedFlags = bannableRelics
            .Select(relic => bannedSet.Contains((relic.CanonicalInstance?.Id ?? relic.Id).ToString()) ? 1 : 0);

        return
        [
            snapshot.EnableExtremeMode ? 1 : 0,
            snapshot.EnableAllPersonas ? 1 : 0,
            snapshot.EnableAllVariantPersonas ? 1 : 0,
            (int)snapshot.StartingPersonaMode,
            (int)snapshot.TokenSeriesMode,
            snapshot.EnablePureAngelMode ? 1 : 0,
            .. bannedFlags
        ];
    }

    private static bool TryDecodeSnapshotChoiceResult(RunState runState, PlayerChoiceResult result,
        out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (AstralChoiceProtocol.TryDecodeIndexedEnvelope(
                result,
                AstralChoiceKind.RunSettingsSnapshot,
                runState,
                SnapshotSessionKey,
                out _,
                out var envelopedPayload))
        {
            return TryDecodeRawSnapshotPayload(envelopedPayload, out snapshot);
        }

        if (!TryGetIndexPayload(result, out var payload))
            return false;

        if (!TryDecodeRawSnapshotPayload(payload, out snapshot))
            return false;

        MainFile.Logger.Info(
            $"{MainFile.ModId} settings sync accepted a legacy/raw snapshot payload because no protocol envelope was present.");
        ShowSyncWarning(
            211,
            "协议兼容",
            "本局联机玩法设置使用了旧版裸 payload 兼容读取。若主客机模组版本不一致，可能导致设置解释分叉。");
        return true;
    }

    private static bool TryDecodeRawSnapshotPayload(IReadOnlyList<int> payload,
        out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        var personaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics();
        var bannableRelics = BannedRelicRegistry.GetCanonicalBannableRelics();
        var isLegacyPayload = payload.Count == personaRelics.Count + 7;
        var isPersonaOnlyPayload = payload.Count == personaRelics.Count + 6;
        var isFullBannablePayload = payload.Count == bannableRelics.Count + 6;
        if (!isLegacyPayload && !isPersonaOnlyPayload && !isFullBannablePayload)
            return false;

        var bannedStartIndex = isLegacyPayload ? 7 : 6;
        var bannedRelicSource = isFullBannablePayload ? bannableRelics : personaRelics;
        var bannedIds = new List<string>();
        for (var i = 0; i < bannedRelicSource.Count && bannedStartIndex + i < payload.Count; i++)
        {
            if (payload[bannedStartIndex + i] == 0)
                continue;

            bannedIds.Add((bannedRelicSource[i].CanonicalInstance?.Id ?? bannedRelicSource[i].Id).ToString());
        }

        var legacyRandomCloneMode = isLegacyPayload && payload[4] != 0;
        snapshot = new ReAstralPartyRunSettingsSnapshot
        {
            EnableExtremeMode = payload[0] != 0,
            EnableAllPersonas = payload[1] != 0,
            EnableAllVariantPersonas = payload[2] != 0,
            StartingPersonaMode = isLegacyPayload
                ? StartingPersonaMode.Standard
                : Enum.IsDefined(typeof(StartingPersonaMode), payload[3])
                    ? (StartingPersonaMode)payload[3]
                    : StartingPersonaMode.Standard,
            TokenSeriesMode = Enum.IsDefined(typeof(TokenSeriesMode), payload[isLegacyPayload ? 5 : 4])
                ? (TokenSeriesMode)payload[isLegacyPayload ? 5 : 4]
                : TokenSeriesMode.RandomTwo,
            EnablePureAngelMode = payload[isLegacyPayload ? 6 : 5] != 0,
            BannedRelicIdsSerialized = bannedIds
        };
        if (isLegacyPayload)
        {
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync decoded legacy starting persona payload and downgraded it to standard mode (legacy_random_clone_mode={legacyRandomCloneMode}).");
            ShowSyncWarning(
                212,
                "旧协议降级",
                $"检测到旧版开局人格设置同步协议，已按标准模式兼容降级处理。legacy_random_clone_mode={legacyRandomCloneMode}。");
        }
        else if (isPersonaOnlyPayload)
        {
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync decoded a persona-only banned relic payload for compatibility.");
            ShowSyncWarning(
                219,
                "旧协议兼容",
                "检测到旧版仅同步人格 ban 位的协议，本局的变体/衍生/Token/其他 ban 位可能未随主机同步。");
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
            ShowSyncWarning(
                213,
                "同步前置",
                "联机玩法设置同步时未拿到 RunManager，已退回安全默认值。");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var netService = runManager.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(localSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            return localSnapshot;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because PlayerChoiceSynchronizer was unavailable; using safe multiplayer defaults.");
            ShowSyncWarning(
                214,
                "同步前置",
                "联机玩法设置同步时未拿到 PlayerChoiceSynchronizer，已退回安全默认值。");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var authorityNetId = ResolveAuthorityNetId(netService);
        if (authorityNetId == 0UL)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because authority net id was unavailable; using safe multiplayer defaults.");
            ShowSyncWarning(
                215,
                "主机识别",
                "联机玩法设置同步时未识别到房主 NetId，已退回安全默认值。");
            state.SetSnapshot(safeSnapshot);
            return safeSnapshot;
        }

        var authorityPlayer = runState.Players.FirstOrDefault(player => player.NetId == authorityNetId);
        if (authorityPlayer == null)
        {
            var safeSnapshot = CreateSafeSnapshot();
            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync skipped because authority player {authorityNetId} was unavailable; using safe multiplayer defaults.");
            ShowSyncWarning(
                216,
                "主机识别",
                $"联机玩法设置同步时未找到房主玩家 {authorityNetId}，已退回安全默认值。");
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
                if (!TryDecodeSnapshotChoiceResult(runState, remoteChoice, out var remoteSnapshot))
                {
                    MainFile.Logger.Warn(
                        $"{MainFile.ModId} settings sync ignored foreign choice from authority player {authorityPlayer.NetId}: choiceId={remoteChoiceId} attempt={attempt + 1} result={remoteChoice}");
                    continue;
                }

                state.SetSnapshot(remoteSnapshot);
                LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
                MainFile.Logger.Info(
                    $"{MainFile.ModId} settings sync received from host player {authorityPlayer.NetId}: extreme_mode={remoteSnapshot.EnableExtremeMode}, all_personas={remoteSnapshot.EnableAllPersonas}, all_variants={remoteSnapshot.EnableAllVariantPersonas}, persona_mode={remoteSnapshot.StartingPersonaMode}, token_series={remoteSnapshot.TokenSeriesMode}, pure_angel={remoteSnapshot.EnablePureAngelMode}, banned_relics={remoteSnapshot.BannedRelicIdsSerialized.Count}");
                return remoteSnapshot;
            }

            MainFile.Logger.Warn(
                $"{MainFile.ModId} settings sync did not receive a valid host snapshot after {MaxClientSnapshotAttempts} attempts; falling back to safe multiplayer defaults.");
            ShowSyncWarning(
                217,
                "接收主机设置",
                $"客户端在 {MaxClientSnapshotAttempts} 次尝试后仍未收到有效的房主玩法设置快照，已退回安全默认值。");
        }

        var choiceId = synchronizer.ReserveChoiceId(authorityPlayer);
        MainFile.Logger.Info(
            $"{MainFile.ModId} settings sync resolved netMode={netService.Type}, host={isHost}, authority={authorityPlayer.NetId}.");

        if (isHost)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(localSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            synchronizer.SyncLocalChoice(authorityPlayer, choiceId, CreateSnapshotChoiceResult(runState, localSnapshot));
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync broadcast by authority player {authorityPlayer.NetId}: extreme_mode={localSnapshot.EnableExtremeMode}, all_personas={localSnapshot.EnableAllPersonas}, all_variants={localSnapshot.EnableAllVariantPersonas}, persona_mode={localSnapshot.StartingPersonaMode}, token_series={localSnapshot.TokenSeriesMode}, pure_angel={localSnapshot.EnablePureAngelMode}, banned_relics={localSnapshot.BannedRelicIdsSerialized.Count}");
            return localSnapshot;
        }

        var fallbackSnapshot = CreateSafeSnapshot();
        MainFile.Logger.Warn(
            $"{MainFile.ModId} settings sync did not have a host authority path; using safe multiplayer defaults.");
        ShowSyncWarning(
            218,
            "主机路径",
            "联机玩法设置同步未进入有效的房主路径，已退回安全默认值。");
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

        public void SetSnapshot(ReAstralPartyRunSettingsSnapshot snapshot)
        {
            lock (_gate)
            {
                Snapshot = snapshot;
            }
        }
    }
}
