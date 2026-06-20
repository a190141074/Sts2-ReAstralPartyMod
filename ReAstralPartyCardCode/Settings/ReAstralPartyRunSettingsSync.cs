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

    public bool EnableStartingRingOfSevenCurses { get; set; }

    public bool EnableStartingPersonaSelection { get; set; } = true;

    public bool EnableDreamSeriesEvents { get; set; } = true;

    public bool EnableEnigmaticSeriesEvents { get; set; } = true;

    public bool EnableMoonPropShopSlots { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;

    public bool EnableLucidDream { get; set; } = true;

    public bool EnableCollectorsCards { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersonas { get; set; }

    public bool EnableVariantPersonas { get; set; } = true;

    public bool EnableAllVariantPersonas { get; set; }

    public StartingPersonaMode StartingPersonaMode { get; set; } = StartingPersonaMode.Standard;

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;

    public bool EnableLucidDreamFalseLifeline { get; set; }

    public bool EnableLucidDreamSmoothSailing { get; set; }

    public bool EnableLucidDreamFishScalesMalice { get; set; }

    public bool EnableLucidDreamSevereWoundOneMalice { get; set; }

    public bool EnableLucidDreamSevereWoundTwoMalice { get; set; }

    public bool EnableLucidDreamMadLifeMalice { get; set; }

    public bool EnableLucidDreamSwampOfFateMalice { get; set; }

    public bool EnableLucidDreamOverpopulationMalice { get; set; }

    public bool EnableLucidDreamCautiousJellyfishMalice { get; set; }

    public bool EnableLucidDreamFaceDeathWithComposure { get; set; }

    public bool EnableLucidDreamWildness { get; set; }

    public bool EnableLucidDreamWildnessPhantom { get; set; }

    public bool EnableLucidDreamPitchBlackImpulse { get; set; }

    public bool EnableLucidDreamBubblePotionOfDreams { get; set; }

    public bool EnableLucidDreamHarmlessWhisper { get; set; }

    public List<string> BannedRelicIdsSerialized { get; set; } = [];

    public List<string> BannedPersonaRelicIdsSerialized
    {
        get => BannedRelicIdsSerialized;
        set => BannedRelicIdsSerialized = value;
    }

    public ReAstralPartyRunSettingsSnapshot Clone()
    {
        return new ReAstralPartyRunSettingsSnapshot
        {
            CurrentContentMode = CurrentContentMode,
            EnableExtremeMode = EnableExtremeMode,
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = EnableMoonPropShopSlots,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableLucidDream = EnableLucidDream,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            StartingPersonaMode = StartingPersonaMode,
            TokenSeriesMode = TokenSeriesMode,
            EnablePureAngelMode = EnablePureAngelMode,
            EnableLucidDreamFalseLifeline = EnableLucidDreamFalseLifeline,
            EnableLucidDreamSmoothSailing = EnableLucidDreamSmoothSailing,
            EnableLucidDreamFishScalesMalice = EnableLucidDreamFishScalesMalice,
            EnableLucidDreamSevereWoundOneMalice = EnableLucidDreamSevereWoundOneMalice,
            EnableLucidDreamSevereWoundTwoMalice = EnableLucidDreamSevereWoundTwoMalice,
            EnableLucidDreamMadLifeMalice = EnableLucidDreamMadLifeMalice,
            EnableLucidDreamSwampOfFateMalice = EnableLucidDreamSwampOfFateMalice,
            EnableLucidDreamOverpopulationMalice = EnableLucidDreamOverpopulationMalice,
            EnableLucidDreamCautiousJellyfishMalice = EnableLucidDreamCautiousJellyfishMalice,
            EnableLucidDreamFaceDeathWithComposure = EnableLucidDreamFaceDeathWithComposure,
            EnableLucidDreamWildness = EnableLucidDreamWildness,
            EnableLucidDreamWildnessPhantom = EnableLucidDreamWildnessPhantom,
            EnableLucidDreamPitchBlackImpulse = EnableLucidDreamPitchBlackImpulse,
            EnableLucidDreamBubblePotionOfDreams = EnableLucidDreamBubblePotionOfDreams,
            EnableLucidDreamHarmlessWhisper = EnableLucidDreamHarmlessWhisper,
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
        var settings = ReAstralPartyModSettingsManager.ReadLocalSettings();
        LobbyGameplaySettingsSync.TryGetSnapshot(out var lobbySnapshot);
        return new ReAstralPartyRunSettingsSnapshot
        {
            CurrentContentMode = ReAstralPartyModSettingsManager.GetCurrentContentMode(),
            EnableStartingInitialPoint = lobbySnapshot?.EnableStartingInitialPoint ?? settings.EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = lobbySnapshot?.EnableStartingAstralRelicStore ?? ReAstralPartyModSettingsManager.EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = lobbySnapshot?.EnableStartingRingOfSevenCurses ?? settings.EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = lobbySnapshot?.EnableStartingPersonaSelection ?? settings.EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = lobbySnapshot?.EnableDreamSeriesEvents ?? settings.EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = lobbySnapshot?.EnableEnigmaticSeriesEvents ?? settings.EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = lobbySnapshot?.EnableMoonPropShopSlots ?? settings.EnableMoonPropShopSlots,
            EnableNeowExtraOption = lobbySnapshot?.EnableNeowExtraOption ?? settings.EnableNeowExtraOption,
            EnableLucidDream = lobbySnapshot?.EnableLucidDream ?? settings.EnableLucidDream,
            EnableCollectorsCards = lobbySnapshot?.EnableCollectorsCards ?? settings.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                lobbySnapshot?.EnableStartingRingOfSevenCurses ?? settings.EnableStartingRingOfSevenCurses,
                lobbySnapshot?.NeowExtraOptionSelectionMode ?? settings.NeowExtraOptionSelectionMode),
            EnableAllPersonas = lobbySnapshot?.EnableAllPersonas ?? settings.EnableAllPersonas,
            EnableVariantPersonas = lobbySnapshot?.EnableVariantPersonas ?? settings.EnableVariantPersonas,
            EnableAllVariantPersonas = lobbySnapshot?.EnableAllVariantPersonas ?? settings.EnableAllVariantPersonas,
            StartingPersonaMode = lobbySnapshot?.StartingPersonaMode
                                  ?? ReAstralPartyModSettingsManager.ResolveStartingPersonaMode(settings),
            TokenSeriesMode = lobbySnapshot?.TokenSeriesMode
                              ?? ReAstralPartyModSettingsManager.ResolveTokenSeriesMode(settings),
            EnableExtremeMode = lobbySnapshot?.EnableExtremeMode ?? settings.EnableExtremeMode,
            EnablePureAngelMode = settings.EnablePureAngelMode,
            EnableLucidDreamFalseLifeline = lobbySnapshot?.EnableLucidDreamFalseLifeline ?? false,
            EnableLucidDreamSmoothSailing = lobbySnapshot?.EnableLucidDreamSmoothSailing ?? false,
            EnableLucidDreamFishScalesMalice = lobbySnapshot?.EnableLucidDreamFishScalesMalice ?? false,
            EnableLucidDreamSevereWoundOneMalice = lobbySnapshot?.EnableLucidDreamSevereWoundOneMalice ?? false,
            EnableLucidDreamSevereWoundTwoMalice = lobbySnapshot?.EnableLucidDreamSevereWoundTwoMalice ?? false,
            EnableLucidDreamMadLifeMalice = lobbySnapshot?.EnableLucidDreamMadLifeMalice ?? false,
            EnableLucidDreamSwampOfFateMalice = lobbySnapshot?.EnableLucidDreamSwampOfFateMalice ?? false,
            EnableLucidDreamOverpopulationMalice = lobbySnapshot?.EnableLucidDreamOverpopulationMalice ?? false,
            EnableLucidDreamCautiousJellyfishMalice = lobbySnapshot?.EnableLucidDreamCautiousJellyfishMalice ?? false,
            EnableLucidDreamFaceDeathWithComposure = lobbySnapshot?.EnableLucidDreamFaceDeathWithComposure ?? false,
            EnableLucidDreamWildness = lobbySnapshot?.EnableLucidDreamWildness ?? false,
            EnableLucidDreamWildnessPhantom = lobbySnapshot?.EnableLucidDreamWildnessPhantom ?? false,
            EnableLucidDreamPitchBlackImpulse = lobbySnapshot?.EnableLucidDreamPitchBlackImpulse ?? false,
            EnableLucidDreamBubblePotionOfDreams = lobbySnapshot?.EnableLucidDreamBubblePotionOfDreams ?? false,
            EnableLucidDreamHarmlessWhisper = lobbySnapshot?.EnableLucidDreamHarmlessWhisper ?? false,
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
            CurrentContentMode = AstralContentMode.Vanilla,
            EnableExtremeMode = false,
            EnableStartingInitialPoint = false,
            EnableStartingAstralRelicStore = true,
            EnableStartingRingOfSevenCurses = false,
            EnableStartingPersonaSelection = true,
            EnableDreamSeriesEvents = true,
            EnableEnigmaticSeriesEvents = true,
            EnableMoonPropShopSlots = true,
            EnableNeowExtraOption = true,
            EnableLucidDream = true,
            EnableCollectorsCards = true,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode.DefaultRandom,
            EnableAllPersonas = false,
            EnableVariantPersonas = true,
            EnableAllVariantPersonas = false,
            StartingPersonaMode = StartingPersonaMode.Standard,
            TokenSeriesMode = TokenSeriesMode.RandomTwo,
            EnablePureAngelMode = false,
            EnableLucidDreamFalseLifeline = false,
            EnableLucidDreamSmoothSailing = false,
            EnableLucidDreamFishScalesMalice = false,
            EnableLucidDreamSevereWoundOneMalice = false,
            EnableLucidDreamSevereWoundTwoMalice = false,
            EnableLucidDreamMadLifeMalice = false,
            EnableLucidDreamSwampOfFateMalice = false,
            EnableLucidDreamOverpopulationMalice = false,
            EnableLucidDreamCautiousJellyfishMalice = false,
            EnableLucidDreamFaceDeathWithComposure = false,
            EnableLucidDreamWildness = false,
            EnableLucidDreamWildnessPhantom = false,
            EnableLucidDreamPitchBlackImpulse = false,
            EnableLucidDreamBubblePotionOfDreams = false,
            EnableLucidDreamHarmlessWhisper = false,
            BannedRelicIdsSerialized = []
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
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync established from lobby snapshot: start_initial_point={lobbySnapshot.EnableStartingInitialPoint}, start_ring_of_seven_curses={lobbySnapshot.EnableStartingRingOfSevenCurses}, start_persona_selection={lobbySnapshot.EnableStartingPersonaSelection}, dream_series={lobbySnapshot.EnableDreamSeriesEvents}, enigmatic_series={lobbySnapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={lobbySnapshot.EnableMoonPropShopSlots}, neow_extra_option={lobbySnapshot.EnableNeowExtraOption}, lucid_dream={lobbySnapshot.EnableLucidDream}, neow_extra_selection={lobbySnapshot.NeowExtraOptionSelectionMode}, all_personas={lobbySnapshot.EnableAllPersonas}, variants_enabled={lobbySnapshot.EnableVariantPersonas}, all_variants={lobbySnapshot.EnableAllVariantPersonas}, persona_mode={lobbySnapshot.StartingPersonaMode}, token_series={lobbySnapshot.TokenSeriesMode}, pure_angel={lobbySnapshot.EnablePureAngelMode}, lucid_malice_flags={CountEnabledLucidDreamMaliceFlags(lobbySnapshot)}, banned_relics={lobbySnapshot.BannedRelicIdsSerialized.Count}");
            return Task.FromResult(lobbySnapshot);
        }

        if (netService.Type == NetGameType.Host)
        {
            var localSnapshot = CreateLocalSnapshot();
            state.SetSnapshot(runState, localSnapshot);
            LobbyGameplaySettingsSync.OnRunSnapshotEstablished();
            LogSnapshotEstablished(runState, "host_runtime_fallback");
            MainFile.Logger.Info(
                $"{MainFile.ModId} settings sync established from host runtime fallback without PlayerChoiceSynchronizer: start_initial_point={localSnapshot.EnableStartingInitialPoint}, start_ring_of_seven_curses={localSnapshot.EnableStartingRingOfSevenCurses}, start_persona_selection={localSnapshot.EnableStartingPersonaSelection}, dream_series={localSnapshot.EnableDreamSeriesEvents}, enigmatic_series={localSnapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={localSnapshot.EnableMoonPropShopSlots}, neow_extra_option={localSnapshot.EnableNeowExtraOption}, lucid_dream={localSnapshot.EnableLucidDream}, neow_extra_selection={localSnapshot.NeowExtraOptionSelectionMode}, all_personas={localSnapshot.EnableAllPersonas}, variants_enabled={localSnapshot.EnableVariantPersonas}, all_variants={localSnapshot.EnableAllVariantPersonas}, persona_mode={localSnapshot.StartingPersonaMode}, token_series={localSnapshot.TokenSeriesMode}, pure_angel={localSnapshot.EnablePureAngelMode}, lucid_malice_flags={CountEnabledLucidDreamMaliceFlags(localSnapshot)}, banned_relics={localSnapshot.BannedRelicIdsSerialized.Count}");
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
            EnableStartingRingOfSevenCurses = lobbySnapshot.EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = lobbySnapshot.EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = lobbySnapshot.EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = lobbySnapshot.EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = lobbySnapshot.EnableMoonPropShopSlots,
            EnableNeowExtraOption = lobbySnapshot.EnableNeowExtraOption,
            EnableLucidDream = lobbySnapshot.EnableLucidDream,
            EnableCollectorsCards = lobbySnapshot.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                lobbySnapshot.EnableStartingRingOfSevenCurses,
                lobbySnapshot.NeowExtraOptionSelectionMode),
            EnableAllPersonas = lobbySnapshot.EnableAllPersonas,
            EnableVariantPersonas = lobbySnapshot.EnableVariantPersonas,
            EnableAllVariantPersonas = lobbySnapshot.EnableAllVariantPersonas,
            StartingPersonaMode = lobbySnapshot.StartingPersonaMode,
            TokenSeriesMode = lobbySnapshot.TokenSeriesMode,
            EnablePureAngelMode = settings.EnablePureAngelMode,
            EnableLucidDreamFalseLifeline = lobbySnapshot.EnableLucidDreamFalseLifeline,
            EnableLucidDreamSmoothSailing = lobbySnapshot.EnableLucidDreamSmoothSailing,
            EnableLucidDreamFishScalesMalice = lobbySnapshot.EnableLucidDreamFishScalesMalice,
            EnableLucidDreamSevereWoundOneMalice = lobbySnapshot.EnableLucidDreamSevereWoundOneMalice,
            EnableLucidDreamSevereWoundTwoMalice = lobbySnapshot.EnableLucidDreamSevereWoundTwoMalice,
            EnableLucidDreamMadLifeMalice = lobbySnapshot.EnableLucidDreamMadLifeMalice,
            EnableLucidDreamSwampOfFateMalice = lobbySnapshot.EnableLucidDreamSwampOfFateMalice,
            EnableLucidDreamOverpopulationMalice = lobbySnapshot.EnableLucidDreamOverpopulationMalice,
            EnableLucidDreamCautiousJellyfishMalice = lobbySnapshot.EnableLucidDreamCautiousJellyfishMalice,
            EnableLucidDreamFaceDeathWithComposure = lobbySnapshot.EnableLucidDreamFaceDeathWithComposure,
            EnableLucidDreamWildness = lobbySnapshot.EnableLucidDreamWildness,
            EnableLucidDreamWildnessPhantom = lobbySnapshot.EnableLucidDreamWildnessPhantom,
            EnableLucidDreamPitchBlackImpulse = lobbySnapshot.EnableLucidDreamPitchBlackImpulse,
            EnableLucidDreamBubblePotionOfDreams = lobbySnapshot.EnableLucidDreamBubblePotionOfDreams,
            EnableLucidDreamHarmlessWhisper = lobbySnapshot.EnableLucidDreamHarmlessWhisper,
            BannedRelicIdsSerialized = [.. (settings.BannedRelicIds.Count > 0
                ? settings.BannedRelicIds
                : settings.BannedPersonaRelicIds ?? [])
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)]
        };
        return true;
    }

    private static int CountEnabledLucidDreamMaliceFlags(ReAstralPartyRunSettingsSnapshot snapshot)
    {
        var count = 0;
        if (snapshot.EnableLucidDreamFalseLifeline)
            count++;
        if (snapshot.EnableLucidDreamSmoothSailing)
            count++;
        if (snapshot.EnableLucidDreamFishScalesMalice)
            count++;
        if (snapshot.EnableLucidDreamSevereWoundOneMalice)
            count++;
        if (snapshot.EnableLucidDreamSevereWoundTwoMalice)
            count++;
        if (snapshot.EnableLucidDreamMadLifeMalice)
            count++;
        if (snapshot.EnableLucidDreamSwampOfFateMalice)
            count++;
        if (snapshot.EnableLucidDreamOverpopulationMalice)
            count++;
        if (snapshot.EnableLucidDreamCautiousJellyfishMalice)
            count++;
        if (snapshot.EnableLucidDreamFaceDeathWithComposure)
            count++;
        if (snapshot.EnableLucidDreamWildness)
            count++;
        if (snapshot.EnableLucidDreamWildnessPhantom)
            count++;
        if (snapshot.EnableLucidDreamPitchBlackImpulse)
            count++;
        if (snapshot.EnableLucidDreamBubblePotionOfDreams)
            count++;
        if (snapshot.EnableLucidDreamHarmlessWhisper)
            count++;
        return count;
    }

    private static void LogSnapshotEstablished(RunState runState, string source)
    {
        var netMode = RunManager.Instance?.NetService?.Type.ToString() ?? "Unknown";
        var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
        var personaWindow = StartingPersonaRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out var reason);
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
