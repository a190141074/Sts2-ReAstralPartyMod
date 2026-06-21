using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Concurrent;
using GodotTranslationServer = Godot.TranslationServer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Telemetry;
using STS2RitsuLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class AstralTelemetry
{
    private enum LegacyTelemetryConsentState
    {
        Unknown = 0,
        Accepted = 1,
        Declined = 2
    }

    private sealed record TelemetryConfig(
        bool Enabled,
        string Provider,
        string Host,
        string ProjectToken,
        string PendingFile);

    private sealed record LegacyTelemetryState(
        LegacyTelemetryConsentState ConsentState,
        bool ConsentPromptShown);

    private sealed record LegacyTelemetryConfig(bool Enabled, string Endpoint);

    private sealed record RunEndedPayload(
        int SchemaVersion,
        string ModId,
        string ModVersion,
        string GameVersion,
        string UploadedAtUtc,
        RunTelemetry Run,
        IReadOnlyList<PlayerRunTelemetry> Players,
        IReadOnlyList<PersonChoiceRecord> PersonChoices,
        IReadOnlyList<TokenChoiceRecord> TokenChoices);

    private sealed record RunTelemetry(
        string RunId,
        string SeedHash,
        bool IsVictory,
        string NetMode,
        int PlayerCount,
        int Ascension,
        int CurrentActIndex,
        int TotalFloor,
        long RunTime);

    private sealed record TelemetryEventRecord(string EventName, Dictionary<string, object?> Properties);

    internal sealed record PersonChoiceRecord(
        int PlayerSlot,
        IReadOnlyList<string> Options,
        string? Selected);

    internal sealed record TokenChoiceRecord(
        int PlayerSlot,
        string Source,
        IReadOnlyList<string> Options,
        string? Selected,
        int RerollCount);

    internal sealed record PlayerRunTelemetry(
        int Slot,
        string Character,
        string? PersonSelected,
        string? PersonSkillCardId,
        int PersonSkillUseCount,
        IReadOnlyList<string> ObtainedTokens);

    private sealed class PlayerTelemetryState
    {
        public required int Slot { get; init; }
        public required string Character { get; init; }
        public string? PersonSelected { get; set; }
        public string? PersonSkillCardId { get; set; }
        public int PersonSkillUseCount { get; set; }
        public HashSet<string> ObtainedTokenIds { get; } = new(StringComparer.Ordinal);
    }

    private const string DefaultProvider = "posthog";
    private const string DefaultHost = "https://us.i.posthog.com";
    private const string DefaultProjectToken = "phc_tUUfTxDRKbzBcq24G78yPUu3w6TYkmCySG4cRwp7bJwC";
    private const string ApplicantId = MainFile.ModId;
    private const string BalanceRequestId = "astral_balance";
    private const string RunHistoryRequestId = "run_history";
    private const string ConfigFileName = "telemetry_config.json";
    private const string LocalConfigFileName = "telemetry_config.local.json";
    private const string StateFileName = "telemetry_state.json";
    private const string ActiveRunFileName = "telemetry_active_run.json";
    private const long MinRunTimeForUploadSeconds = 180;

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly object StateLock = new();
    private static readonly object ConfigLock = new();
    private static readonly HashSet<string> SubmittedRunIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, RunTelemetryState> ActiveRuns = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> LocalizationTableCache =
        new(StringComparer.OrdinalIgnoreCase);
    private static TelemetryConfig? _cachedConfig;
    private static ITelemetryClient? _telemetryClient;
    private static bool _applicantRegistered;
    private static bool _lifecycleBridgeRegistered;
    private static string? _cachedManifestModVersion;

    private sealed class RunTelemetryState
    {
        public string RunKey { get; init; } = string.Empty;
        public string Seed { get; init; } = string.Empty;
        public Dictionary<ulong, PlayerTelemetryState> PlayerStates { get; } = new();
        public List<PersonChoiceRecord> PersonChoices { get; } = [];
        public List<TokenChoiceRecord> TokenChoices { get; } = [];
    }

    private sealed record ActiveRunSnapshot(
        string RunKey,
        string Seed,
        IReadOnlyList<PlayerTelemetrySnapshot> Players,
        IReadOnlyList<PersonChoiceRecord> PersonChoices,
        IReadOnlyList<TokenChoiceRecord> TokenChoices);

    private sealed record PlayerTelemetrySnapshot(
        ulong NetId,
        int Slot,
        string Character,
        string? PersonSelected,
        string? PersonSkillCardId,
        int PersonSkillUseCount,
        IReadOnlyList<string> ObtainedTokens);

    private static void RegisterApplicantIfNeeded()
    {
        if (_applicantRegistered)
            return;

        var config = LoadConfig();
        RitsuLibFramework.RegisterTelemetryApplicant(new TelemetryApplicant
        {
            ApplicantId = ApplicantId,
            OwnerModId = MainFile.ModId,
            DisplayName = "Astral Party Mod",
            DisplayNameText = ModSettingsText.LocString(
                "settings_ui",
                "RE_ASTRAL_PARTY_MOD_SETTINGS.mod_display_name",
                "Astral Party Mod"),
            Adapter = CreateAdapter(config),
            Requests =
            [
                TelemetryRequest.Custom(
                    BalanceRequestId,
                    ModSettingsText.LocString(
                        "settings_ui",
                        "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.description",
                        "Allow anonymous usage statistics for personas, tokens, and skill cards.")),
                TelemetryRequest.RunHistory(
                    ModSettingsText.LocString(
                        "settings_ui",
                        "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry_run_history.description",
                        "Allow anonymized vanilla run history to be uploaded for verification and diagnostics."),
                    captureFilter: ShouldCaptureRunHistory)
            ]
        });

        _telemetryClient = RitsuLibFramework.GetTelemetryClient(ApplicantId);
        _applicantRegistered = true;
        TryMigrateLegacyConsent();
    }

    private static ITelemetryAdapter CreateAdapter(TelemetryConfig config)
    {
        if (string.Equals(config.Provider, DefaultProvider, StringComparison.OrdinalIgnoreCase))
            return new PostHogTelemetryAdapter(config.Host, config.ProjectToken);

        return new HttpJsonTelemetryAdapter(config.Host);
    }

    private static ITelemetryClient GetTelemetryClient()
    {
        RegisterApplicantIfNeeded();
        return _telemetryClient ??= RitsuLibFramework.GetTelemetryClient(ApplicantId);
    }

    private static bool TrySetRitsuLibConsent(bool enabled)
    {
        try
        {
            RegisterApplicantIfNeeded();
            IEnumerable<string>? grantedRequests = enabled ? [BalanceRequestId, RunHistoryRequestId] : null;
            var consentState = enabled ? TelemetryConsentState.Granted : TelemetryConsentState.Denied;
            RitsuLibFramework.SetTelemetryApplicantConsent(ApplicantId, consentState, grantedRequests);
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Consent bridge failed: {ex.Message}");
            return false;
        }
    }

    private static void TryMigrateLegacyConsent()
    {
        try
        {
            var path = GetStatePath();
            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            var legacyState = JsonSerializer.Deserialize<LegacyTelemetryState>(json, JsonOptions);
            if (legacyState == null || legacyState.ConsentState == LegacyTelemetryConsentState.Unknown)
                return;

            TrySetRitsuLibConsent(legacyState.ConsentState == LegacyTelemetryConsentState.Accepted);
            File.Delete(path);
            MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Migrated legacy telemetry consent into RitsuLib.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Legacy consent migration failed: {ex.Message}");
        }
    }

    public static void Initialize()
    {
        try
        {
            EnsureConfigFile();
            RegisterLifecycleBridgeIfNeeded();
            RegisterApplicantIfNeeded();
            SyncFromModSettings();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Config init failed: {ex.Message}");
            ShowTelemetryErrorToast("匿名数据初始化失败", ex.Message);
        }
    }

    public static void SyncFromModSettings()
    {
        try
        {
            var enabled = ReAstralPartyModSettingsManager.EnableTelemetry;
            SetCollectionEnabledByConsent(enabled);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}][Telemetry] Failed to sync telemetry setting from mod settings: {ex.Message}");
            ShowTelemetryWarningToast("匿名数据设置同步失败", ex.Message);
        }
    }

    public static bool ShouldShowConsentPrompt()
    {
        return false;
    }

    public static void SetCollectionEnabledByConsent(bool enabled)
    {
        if (!TrySetRitsuLibConsent(enabled))
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}][Telemetry] Failed to bridge telemetry consent to RitsuLib; local setting may not take effect immediately.");
            ShowTelemetryWarningToast("匿名数据授权同步失败", "RitsuLib 遥测授权状态未能立即更新。");
        }

        MainFile.Logger.Info(
            $"[{MainFile.ModId}][Telemetry] User consent updated: enabled={enabled}");
    }

    public static bool IsCollectionEnabled()
    {
        return ReAstralPartyModSettingsManager.EnableTelemetry &&
               GetTelemetryClient().IsEnabled(BalanceRequestId);
    }

    private static void RegisterLifecycleBridgeIfNeeded()
    {
        if (_lifecycleBridgeRegistered)
            return;

        _lifecycleBridgeRegistered = true;
        RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunEndedEvent>(OnRunEndedFromLifecycle, replayCurrentState: false);
    }

    private static void OnRunStarted(RunStartedEvent evt)
    {
        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.StartRunBootstrap, "telemetry start run"))
            return;

        BeginNewRun(evt.RunState);
    }

    private static void OnRunLoaded(RunLoadedEvent evt)
    {
        RestoreLoadedRun(evt.RunState);
    }

    private static void OnRunEndedFromLifecycle(RunEndedEvent evt)
    {
        if (evt.IsAbandoned)
        {
            DiscardPersistedRunState("abandon_run");
            return;
        }

        OnRunEnded(RunManager.Instance?.DebugOnlyGetState(), evt.Run, evt.IsVictory);
    }

    public static void BeginNewRun(RunState? runState)
    {
        if (runState == null)
            return;

        lock (StateLock)
        {
            var state = CreateRunState(runState);
            ActiveRuns.Clear();
            ActiveRuns[state.RunKey] = state;
            PersistRunStateLocked(state);
        }
    }

    public static void RestoreLoadedRun(RunState? runState)
    {
        if (runState == null)
            return;

        lock (StateLock)
        {
            var runKey = GetRunKey(runState);
            var state = CreateRunState(runState);
            var snapshot = TryReadActiveSnapshotLocked();
            if (snapshot != null && string.Equals(snapshot.RunKey, runKey, StringComparison.Ordinal))
            {
                ApplySnapshotToState(state, snapshot);
                MainFile.Logger.Info(
                    $"[{MainFile.ModId}][Telemetry] Restored active run snapshot runKey={runKey} personChoices={state.PersonChoices.Count} tokenChoices={state.TokenChoices.Count}.");
            }
            else
            {
                if (snapshot != null)
                    MainFile.Logger.Info(
                        $"[{MainFile.ModId}][Telemetry] Active run snapshot runKey mismatch; expected={runKey} actual={snapshot.RunKey}. Resetting snapshot.");

                PersistRunStateLocked(state);
            }

            ActiveRuns.Clear();
            ActiveRuns[runKey] = state;
            PersistRunStateLocked(state);
        }
    }

    public static void DiscardPersistedRunState(string reason)
    {
        lock (StateLock)
        {
            ActiveRuns.Clear();
            DeleteActiveRunSnapshotLocked();
            MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Discarded active run snapshot reason={reason}.");
        }
    }

    public static void DiscardPersistedRunStateIfNoActiveRun(string reason)
    {
        lock (StateLock)
        {
            if (RunManager.Instance?.DebugOnlyGetState() != null)
                return;

            ActiveRuns.Clear();
            DeleteActiveRunSnapshotLocked();
            MainFile.Logger.Info(
                $"[{MainFile.ModId}][Telemetry] Discarded active run snapshot without live run reason={reason}.");
        }
    }

    public static void RecordPersonaChoice(RunState runState, IReadOnlyList<RelicModel> options,
        IReadOnlyDictionary<ulong, int> selectedIndexes)
    {
        var state = GetOrCreateRunState(runState);
        lock (StateLock)
        {
            state.PersonChoices.Clear();
            foreach (var player in runState.Players.OrderBy(static player => player.NetId))
            {
                var slot = GetPlayerSlot(runState, player);
                var optionIds = options.Select(GetRelicId).ToArray();
                string? selected = null;
                if (selectedIndexes.TryGetValue(player.NetId, out var selectedIndex)
                    && selectedIndex >= 0
                    && selectedIndex < options.Count)
                    selected = GetRelicId(options[selectedIndex]);

                state.PersonChoices.Add(new PersonChoiceRecord(slot, optionIds, selected));
                if (state.PlayerStates.TryGetValue(player.NetId, out var playerState))
                {
                    playerState.PersonSelected = selected;
                    playerState.PersonSkillCardId =
                        selected == null ? null : PersonSkillRegistry.TryGetPersonSkillCardId(selected);
                }
            }

            MainFile.Logger.Info(
                $"[{MainFile.ModId}][Telemetry] Recorded person choices: players={state.PersonChoices.Count} runKey={state.RunKey}");
            PersistRunStateLocked(state);
        }
    }

    public static void RecordTokenChoice(Player player, string source, IReadOnlyList<RelicModel> options,
        RelicModel? selectedRelic, int rerollCount)
    {
        if (player.RunState is not RunState runState)
            return;

        if (options.Count == 0)
            return;

        var canonicalOptions = options.Select(static relic => relic.CanonicalInstance ?? relic).ToList();
        if (canonicalOptions.All(static relic => !TokenRelicRegistry.IsTokenRelic(relic)))
            return;

        var state = GetOrCreateRunState(runState);
        lock (StateLock)
        {
            state.TokenChoices.Add(new TokenChoiceRecord(
                GetPlayerSlot(runState, player),
                source,
                canonicalOptions.Select(GetRelicId).ToArray(),
                selectedRelic == null ? null : GetRelicId(selectedRelic),
                Math.Max(0, rerollCount)));
            PersistRunStateLocked(state);
        }
    }

    public static void RecordObtainedToken(Player owner, RelicModel relic)
    {
        if (owner.RunState is not RunState runState)
            return;

        var canonicalRelic = relic.CanonicalInstance ?? relic;
        if (!TokenRelicRegistry.IsTokenRelic(canonicalRelic))
            return;

        var state = GetOrCreateRunState(runState);
        lock (StateLock)
        {
            if (state.PlayerStates.TryGetValue(owner.NetId, out var playerState))
            {
                playerState.ObtainedTokenIds.Add(GetRelicId(canonicalRelic));
                PersistRunStateLocked(state);
            }
        }
    }

    public static void RecordPersonaSkillUse(CardModel? card)
    {
        if (card?.Owner == null || card.Owner.RunState is not RunState runState)
            return;

        var canonicalCard = card.CanonicalInstance ?? card;
        if (!PersonSkillRegistry.IsTrackedPersonSkillCard(canonicalCard))
            return;

        var state = GetOrCreateRunState(runState);
        lock (StateLock)
        {
            if (!state.PlayerStates.TryGetValue(card.Owner.NetId, out var playerState))
                return;

            if (playerState.PersonSkillCardId == null)
                playerState.PersonSkillCardId = canonicalCard.Id.Entry;

            if (!string.Equals(playerState.PersonSkillCardId, canonicalCard.Id.Entry, StringComparison.Ordinal))
                return;

            playerState.PersonSkillUseCount++;
            PersistRunStateLocked(state);
        }
    }

    public static void RecordPersonaSkillUseIfTracked(CardModel? card)
    {
        if (card == null)
            return;

        RecordPersonaSkillUse(card);
    }

    public static void OnRunEnded(RunState? runState, SerializableRun serializableRun, bool isVictory)
    {
        try
        {
            var config = LoadConfig();
            if (!config.Enabled || !IsCollectionEnabled())
                return;

            if (serializableRun.RunTime <= MinRunTimeForUploadSeconds)
            {
                MainFile.Logger.Info(
                    $"[{MainFile.ModId}][Telemetry] Upload skipped for short run runTime={serializableRun.RunTime}s");
                return;
            }

            var gameType = RunManager.Instance.NetService.Type;
            if (gameType is NetGameType.Client or NetGameType.Replay)
            {
                MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Upload skipped for netMode={gameType}");
                return;
            }
            if (gameType is not (NetGameType.Singleplayer or NetGameType.Host or NetGameType.None))
            {
                MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Upload skipped for unsupported netMode={gameType}");
                return;
            }

            var payload = BuildPayload(runState, serializableRun, isVictory, gameType);
            if (payload == null)
                return;

            if (!SubmittedRunIds.Add(payload.Run.RunId))
                return;

            PublishPayload(payload, serializableRun);
            MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Queued run={payload.Run.RunId} via RitsuLib telemetry.");
            ShowTelemetrySuccessToast();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Upload scheduling failed: {ex.Message}");
            ShowTelemetryErrorToast("匿名数据推送失败", ex.Message);
        }
        finally
        {
            if (runState != null)
                lock (StateLock)
                {
                    ActiveRuns.Remove(GetRunKey(runState));
                    DeleteActiveRunSnapshotLocked();
                }
        }
    }

    private static void PublishPayload(RunEndedPayload payload, SerializableRun serializableRun)
    {
        var client = GetTelemetryClient();
        foreach (var evt in BuildTelemetryEvents(payload))
            client.Capture(evt.EventName, BalanceRequestId, evt.Properties);

        if (client.IsEnabled(RunHistoryRequestId))
            TelemetryApi.CaptureVanillaRunHistory(
                ApplicantId,
                JsonSerializer.SerializeToNode(serializableRun, JsonOptions) ?? new JsonObject(),
                applicantPayload: BuildRunHistoryModPayload(payload),
                properties: BuildRunHistoryProperties(payload));
    }

    private static RunEndedPayload? BuildPayload(RunState? runState, SerializableRun serializableRun, bool isVictory,
        NetGameType gameType)
    {
        if (runState == null)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Payload skipped: runState unavailable");
            return null;
        }

        RunTelemetryState state;
        lock (StateLock)
        {
            state = GetOrCreateRunState(runState);
        }

        var seed = runState.Rng.StringSeed ?? string.Empty;
        var seedHash = Sha256Hex("seed|" + seed);
        var runIdParts = new[]
        {
            "re-astral-party-run-v1",
            seed,
            runState.AscensionLevel.ToString(),
            runState.Players.Count.ToString(),
            string.Join(",", runState.Players.Select(static player => player.Character.Id.Entry))
        };
        var runId = Sha256Hex(string.Join("|", runIdParts));

        var players = runState.Players
            .OrderBy(static player => player.NetId)
            .Select(player =>
            {
                state.PlayerStates.TryGetValue(player.NetId, out var playerState);
                return new PlayerRunTelemetry(
                    GetPlayerSlot(runState, player),
                    player.Character.Id.Entry,
                    playerState?.PersonSelected,
                    playerState?.PersonSkillCardId,
                    playerState?.PersonSkillUseCount ?? 0,
                    playerState?.ObtainedTokenIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray() ?? []);
            })
            .ToArray();

        MainFile.Logger.Info(
            $"[{MainFile.ModId}][Telemetry] Built payload: personChoices={state.PersonChoices.Count} tokenChoices={state.TokenChoices.Count} players={players.Length} runKey={state.RunKey}");

        return new RunEndedPayload(
            1,
            MainFile.ModId,
            GetModVersion(),
            GetGameVersion(),
            DateTimeOffset.UtcNow.ToString("O"),
            new RunTelemetry(
                runId,
                seedHash,
                isVictory,
                gameType.ToString(),
                runState.Players.Count,
                runState.AscensionLevel,
                runState.CurrentActIndex,
                runState.TotalFloor,
                serializableRun.RunTime),
            players,
            state.PersonChoices.ToArray(),
            state.TokenChoices.ToArray());
    }

    private static RunTelemetryState GetOrCreateRunState(RunState runState)
    {
        var runKey = GetRunKey(runState);
        if (ActiveRuns.TryGetValue(runKey, out var existing))
            return existing;

        var created = CreateRunState(runState);
        ActiveRuns[runKey] = created;
        PersistRunStateLocked(created);
        return created;
    }

    private static RunTelemetryState CreateRunState(RunState runState)
    {
        var state = new RunTelemetryState
        {
            RunKey = GetRunKey(runState),
            Seed = runState.Rng.StringSeed ?? string.Empty
        };

        foreach (var player in runState.Players.OrderBy(static player => player.NetId))
            state.PlayerStates[player.NetId] = new PlayerTelemetryState
            {
                Slot = GetPlayerSlot(runState, player),
                Character = player.Character.Id.Entry
            };

        return state;
    }

    private static void PersistRunStateLocked(RunTelemetryState state)
    {
        try
        {
            Directory.CreateDirectory(GetDataDirectory());
            var snapshot = CreateSnapshot(state);
            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            var path = GetActiveRunPath();
            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, json, Encoding.UTF8);
            File.Move(tempPath, path, true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to persist active run snapshot: {ex.Message}");
        }
    }

    private static ActiveRunSnapshot CreateSnapshot(RunTelemetryState state)
    {
        var players = state.PlayerStates
            .OrderBy(static pair => pair.Key)
            .Select(pair => new PlayerTelemetrySnapshot(
                pair.Key,
                pair.Value.Slot,
                pair.Value.Character,
                pair.Value.PersonSelected,
                pair.Value.PersonSkillCardId,
                pair.Value.PersonSkillUseCount,
                pair.Value.ObtainedTokenIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray()))
            .ToArray();

        return new ActiveRunSnapshot(
            state.RunKey,
            state.Seed,
            players,
            state.PersonChoices.ToArray(),
            state.TokenChoices.ToArray());
    }

    private static ActiveRunSnapshot? TryReadActiveSnapshotLocked()
    {
        try
        {
            var path = GetActiveRunPath();
            if (!File.Exists(path))
                return null;

            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<ActiveRunSnapshot>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to restore active run snapshot: {ex.Message}");
            return null;
        }
    }

    private static void ApplySnapshotToState(RunTelemetryState state, ActiveRunSnapshot snapshot)
    {
        state.PersonChoices.Clear();
        state.PersonChoices.AddRange(snapshot.PersonChoices ?? []);
        state.TokenChoices.Clear();
        state.TokenChoices.AddRange(snapshot.TokenChoices ?? []);

        foreach (var player in snapshot.Players ?? [])
        {
            if (!state.PlayerStates.TryGetValue(player.NetId, out var playerState))
                continue;

            playerState.PersonSelected = player.PersonSelected;
            playerState.PersonSkillCardId = player.PersonSkillCardId;
            playerState.PersonSkillUseCount = Math.Max(0, player.PersonSkillUseCount);
            playerState.ObtainedTokenIds.Clear();
            foreach (var tokenId in player.ObtainedTokens.Where(static id => !string.IsNullOrWhiteSpace(id)))
                playerState.ObtainedTokenIds.Add(tokenId);
        }
    }

    private static void DeleteActiveRunSnapshotLocked()
    {
        try
        {
            var path = GetActiveRunPath();
            var tempPath = path + ".tmp";
            if (File.Exists(path))
                File.Delete(path);
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to delete active run snapshot: {ex.Message}");
        }
    }

    private static TelemetryConfig LoadConfig()
    {
        lock (ConfigLock)
        {
            return LoadConfigCore();
        }
    }

    private static void EnsureConfigFile()
    {
        var configPath = GetConfigPath();
        Directory.CreateDirectory(GetDataDirectory());

        if (!File.Exists(configPath))
        {
            var config = new TelemetryConfig(true, DefaultProvider, DefaultHost, DefaultProjectToken, string.Empty);
            WriteConfigFile(config);
        }
    }

    private static string GetDataDirectory()
    {
        try
        {
            var godotUserDir = Godot.OS.GetUserDataDir();
            if (!string.IsNullOrWhiteSpace(godotUserDir))
                return Path.Combine(godotUserDir, MainFile.ModId);
        }
        catch
        {
        }

        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(baseDir, "SlayTheSpire2", MainFile.ModId);
    }

    private static string GetConfigPath()
    {
        return Path.Combine(GetDataDirectory(), ConfigFileName);
    }

    private static string GetLocalConfigPath()
    {
        return Path.Combine(GetDataDirectory(), LocalConfigFileName);
    }

    private static string GetStatePath()
    {
        return Path.Combine(GetDataDirectory(), StateFileName);
    }

    private static string GetActiveRunPath()
    {
        return Path.Combine(GetDataDirectory(), ActiveRunFileName);
    }

    private static int GetPlayerSlot(RunState runState, Player player)
    {
        for (var i = 0; i < runState.Players.Count; i++)
            if (ReferenceEquals(runState.Players[i], player))
                return i;

        return Math.Max(0, runState.GetPlayerSlotIndex(player));
    }

    private static string GetRunKey(RunState runState)
    {
        var seed = runState.Rng.StringSeed ?? string.Empty;
        var ascension = runState.AscensionLevel;
        var playerCount = runState.Players.Count;
        var characters = string.Join(
            ",",
            runState.Players.Select(static player => player.Character.Id.Entry ?? "<unknown>"));
        return $"{seed}|a{ascension}|p{playerCount}|{characters}";
    }

    private static string GetRelicId(RelicModel relic)
    {
        return (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
    }

    private static string GetModVersion()
    {
        if (!string.IsNullOrWhiteSpace(_cachedManifestModVersion))
            return _cachedManifestModVersion;

        try
        {
            var manifestPath = TryGetModManifestPath();
            if (!string.IsNullOrWhiteSpace(manifestPath) && File.Exists(manifestPath))
            {
                using var stream = File.OpenRead(manifestPath);
                using var document = JsonDocument.Parse(stream);
                if (document.RootElement.ValueKind == JsonValueKind.Object &&
                    document.RootElement.TryGetProperty("version", out var versionElement) &&
                    versionElement.ValueKind == JsonValueKind.String)
                {
                    var manifestVersion = versionElement.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(manifestVersion))
                    {
                        _cachedManifestModVersion = manifestVersion;
                        return manifestVersion;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to read mod manifest version: {ex.Message}");
        }

        return typeof(MainFile).Assembly.GetName().Version?.ToString() ?? "unknown";
    }

    private static string GetGameVersion()
    {
        return Godot.ProjectSettings.GetSetting("application/config/version", "unknown").AsString();
    }

    private static void WriteConfigFile(TelemetryConfig config)
    {
        File.WriteAllText(
            GetConfigPath(),
            JsonSerializer.Serialize(config, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
    }

    private static TelemetryConfig LoadConfigCore()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        EnsureConfigFile();

        var localPath = GetLocalConfigPath();
        var basePath = GetConfigPath();
        try
        {
            if (File.Exists(localPath))
            {
                var localJson = File.ReadAllText(localPath);
                var localConfig = JsonSerializer.Deserialize<TelemetryConfig>(localJson, JsonOptions);
                if (IsValidConfig(localConfig))
                {
                    _cachedConfig = NormalizeConfig(localConfig!);
                    return _cachedConfig;
                }
            }

            var json = File.ReadAllText(basePath);
            var config = JsonSerializer.Deserialize<TelemetryConfig>(json, JsonOptions);
            if (IsValidConfig(config))
            {
                _cachedConfig = NormalizeConfig(config!);
                return _cachedConfig;
            }

            var legacyConfig = JsonSerializer.Deserialize<LegacyTelemetryConfig>(json, JsonOptions);
            if (legacyConfig != null)
            {
                var migrated = new TelemetryConfig(
                    legacyConfig.Enabled,
                    DefaultProvider,
                    DefaultHost,
                    DefaultProjectToken,
                    string.Empty);
                WriteConfigFile(migrated);
                MainFile.Logger.Info(
                    $"[{MainFile.ModId}][Telemetry] Migrated legacy telemetry config to PostHog format.");
                _cachedConfig = migrated;
                return _cachedConfig;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Config read failed: {ex.Message}");
        }

        _cachedConfig = new TelemetryConfig(true, DefaultProvider, DefaultHost, DefaultProjectToken, string.Empty);
        return _cachedConfig;
    }

    private static bool IsValidConfig(TelemetryConfig? config)
    {
        return config != null
               && !string.IsNullOrWhiteSpace(config.Host)
               && !string.IsNullOrWhiteSpace(config.ProjectToken);
    }

    private static TelemetryConfig NormalizeConfig(TelemetryConfig config)
    {
        return config with
        {
            Provider = string.IsNullOrWhiteSpace(config.Provider) ? DefaultProvider : config.Provider,
            Host = string.IsNullOrWhiteSpace(config.Host) ? DefaultHost : config.Host.TrimEnd('/'),
            ProjectToken = config.ProjectToken.Trim(),
            PendingFile = config.PendingFile ?? string.Empty
        };
    }

    private static Dictionary<string, object?> CreateBaseProperties(
        RunEndedPayload payload,
        int playerSlot,
        PlayerRunTelemetry? player,
        string? personaSelected)
    {
        var locale = GetCurrentClientLocale();
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["$process_person_profile"] = false,
            ["mod_id"] = payload.ModId,
            ["mod_version"] = payload.ModVersion,
            ["game_version"] = payload.GameVersion,
            ["schema_version"] = payload.SchemaVersion,
            ["uploaded_at_utc"] = payload.UploadedAtUtc,
            ["run_id"] = payload.Run.RunId,
            ["seed_hash"] = payload.Run.SeedHash,
            ["player_slot"] = playerSlot,
            ["client_locale"] = locale,
            ["character"] = player?.Character,
            ["persona_selected"] = personaSelected,
            ["persona_selected_label"] = GetRelicLabel(personaSelected),
            ["persona_selected_label_zhs"] = GetRelicLabel(personaSelected, "zhs"),
            ["persona_selected_label_en"] = GetRelicLabel(personaSelected, "eng")
        };
    }

    private static JsonObject BuildRunHistoryModPayload(RunEndedPayload payload)
    {
        return new JsonObject
        {
            ["astral_run_payload"] = JsonSerializer.SerializeToNode(payload, JsonOptions)
        };
    }

    private static Dictionary<string, object?> BuildRunHistoryProperties(RunEndedPayload payload)
    {
        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["payload_kind"] = "astral_run_history_with_mod_payload",
            ["astral_run_id"] = payload.Run.RunId,
            ["astral_net_mode"] = payload.Run.NetMode,
            ["astral_player_count"] = payload.Run.PlayerCount,
            ["astral_is_victory"] = payload.Run.IsVictory,
            ["astral_run_time"] = payload.Run.RunTime
        };
    }

    private static bool ShouldCaptureRunHistory(RunEndedEvent evt)
    {
        if (!ReAstralPartyModSettingsManager.EnableTelemetry)
            return false;

        if (evt.Run.RunTime <= MinRunTimeForUploadSeconds)
            return false;

        return RunManager.Instance?.NetService.Type is NetGameType.None or NetGameType.Singleplayer or NetGameType.Host;
    }

    private static void ShowTelemetrySuccessToast()
    {
        AstralNotificationService.ShowInfo(AstralNotificationModule.Telemetry, "匿名数据推送成功", "遥测");
    }

    private static void ShowTelemetryWarningToast(string body, string? title = null)
    {
        AstralNotificationService.ShowWarning(AstralNotificationModule.Telemetry, body, title);
    }

    private static void ShowTelemetryErrorToast(string body, string? title = null)
    {
        AstralNotificationService.ShowError(AstralNotificationModule.Telemetry, body, title);
    }

    private static IEnumerable<TelemetryEventRecord> BuildTelemetryEvents(RunEndedPayload payload)
    {
        foreach (var choice in payload.PersonChoices)
        {
            foreach (var optionEvent in CreatePersonaOptionOfferedEvents(payload, choice))
                yield return optionEvent;

            yield return CreatePersonChoiceEvent(payload, choice);
        }

        foreach (var choice in payload.TokenChoices)
        {
            foreach (var optionEvent in CreateTokenOptionOfferedEvents(payload, choice))
                yield return optionEvent;

            yield return CreateTokenChoiceEvent(payload, choice);
        }

        foreach (var player in payload.Players)
        {
            foreach (var tokenId in player.ObtainedTokens)
                yield return CreateTokenObtainedEvent(payload, player, tokenId);

            yield return CreateRunFinishedEvent(payload, player);
        }
    }

    private static IEnumerable<TelemetryEventRecord> CreatePersonaOptionOfferedEvents(
        RunEndedPayload payload,
        PersonChoiceRecord choice)
    {
        foreach (var optionId in choice.Options)
            yield return new TelemetryEventRecord(
                "astral_persona_option_offered",
                CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                    .With("option_id", optionId)
                    .With("option_label", GetRelicLabel(optionId))
                    .With("option_label_zhs", GetRelicLabel(optionId, "zhs"))
                    .With("option_label_en", GetRelicLabel(optionId, "eng"))
                    .With("selected", choice.Selected)
                    .With("selected_label", GetRelicLabel(choice.Selected))
                    .With("selected_label_zhs", GetRelicLabel(choice.Selected, "zhs"))
                    .With("selected_label_en", GetRelicLabel(choice.Selected, "eng"))
                    .With("is_selected", string.Equals(optionId, choice.Selected, StringComparison.Ordinal)));
    }

    private static IEnumerable<TelemetryEventRecord> CreateTokenOptionOfferedEvents(
        RunEndedPayload payload,
        TokenChoiceRecord choice)
    {
        foreach (var optionId in choice.Options)
            yield return new TelemetryEventRecord(
                "astral_token_option_offered",
                CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                    .With("source", choice.Source)
                    .With("option_id", optionId)
                    .With("option_label", GetRelicLabel(optionId))
                    .With("option_label_zhs", GetRelicLabel(optionId, "zhs"))
                    .With("option_label_en", GetRelicLabel(optionId, "eng"))
                    .With("selected", choice.Selected)
                    .With("selected_label", GetRelicLabel(choice.Selected))
                    .With("selected_label_zhs", GetRelicLabel(choice.Selected, "zhs"))
                    .With("selected_label_en", GetRelicLabel(choice.Selected, "eng"))
                    .With("is_selected", string.Equals(optionId, choice.Selected, StringComparison.Ordinal))
                    .With("reroll_count", choice.RerollCount));
    }

    private static TelemetryEventRecord CreatePersonChoiceEvent(RunEndedPayload payload, PersonChoiceRecord choice)
    {
        var optionLabels = choice.Options.Select(GetRelicLabel).ToArray();
        var optionLabelsZhs = choice.Options.Select(optionId => GetRelicLabel(optionId, "zhs")).ToArray();
        var optionLabelsEn = choice.Options.Select(optionId => GetRelicLabel(optionId, "eng")).ToArray();
        return new TelemetryEventRecord(
            "astral_persona_choice",
            CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                .With("options", choice.Options.ToArray())
                .With("option_labels", optionLabels)
                .With("option_labels_zhs", optionLabelsZhs)
                .With("option_labels_en", optionLabelsEn)
                .With("selected", choice.Selected)
                .With("selected_label", GetRelicLabel(choice.Selected))
                .With("selected_label_zhs", GetRelicLabel(choice.Selected, "zhs"))
                .With("selected_label_en", GetRelicLabel(choice.Selected, "eng")));
    }

    private static TelemetryEventRecord CreateTokenChoiceEvent(RunEndedPayload payload, TokenChoiceRecord choice)
    {
        var optionLabels = choice.Options.Select(GetRelicLabel).ToArray();
        var optionLabelsZhs = choice.Options.Select(optionId => GetRelicLabel(optionId, "zhs")).ToArray();
        var optionLabelsEn = choice.Options.Select(optionId => GetRelicLabel(optionId, "eng")).ToArray();
        return new TelemetryEventRecord(
            "astral_token_choice",
            CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                .With("source", choice.Source)
                .With("options", choice.Options.ToArray())
                .With("option_labels", optionLabels)
                .With("option_labels_zhs", optionLabelsZhs)
                .With("option_labels_en", optionLabelsEn)
                .With("selected", choice.Selected)
                .With("selected_label", GetRelicLabel(choice.Selected))
                .With("selected_label_zhs", GetRelicLabel(choice.Selected, "zhs"))
                .With("selected_label_en", GetRelicLabel(choice.Selected, "eng"))
                .With("reroll_count", choice.RerollCount));
    }

    private static TelemetryEventRecord CreateTokenObtainedEvent(
        RunEndedPayload payload,
        PlayerRunTelemetry player,
        string tokenId)
    {
        return new TelemetryEventRecord(
            "astral_token_obtained",
            CreateBaseProperties(payload, player.Slot, player, player.PersonSelected)
                .With("token_id", tokenId)
                .With("token_label", GetRelicLabel(tokenId))
                .With("token_label_zhs", GetRelicLabel(tokenId, "zhs"))
                .With("token_label_en", GetRelicLabel(tokenId, "eng"))
                .With("is_victory", payload.Run.IsVictory)
                .With("run_time", payload.Run.RunTime)
                .With("ascension", payload.Run.Ascension)
                .With("current_act_index", payload.Run.CurrentActIndex)
                .With("total_floor", payload.Run.TotalFloor)
                .With("net_mode", payload.Run.NetMode)
                .With("player_count", payload.Run.PlayerCount));
    }

    private static TelemetryEventRecord CreateRunFinishedEvent(RunEndedPayload payload, PlayerRunTelemetry player)
    {
        return new TelemetryEventRecord(
            "astral_run_finished",
            CreateBaseProperties(payload, player.Slot, player, player.PersonSelected)
                .With("character", player.Character)
                .With("persona_selected", player.PersonSelected)
                .With("persona_selected_label", GetRelicLabel(player.PersonSelected))
                .With("persona_selected_label_zhs", GetRelicLabel(player.PersonSelected, "zhs"))
                .With("persona_selected_label_en", GetRelicLabel(player.PersonSelected, "eng"))
                .With("persona_skill_card_id", player.PersonSkillCardId)
                .With("persona_skill_card_label", GetCardLabel(player.PersonSkillCardId))
                .With("persona_skill_card_label_zhs", GetCardLabel(player.PersonSkillCardId, "zhs"))
                .With("persona_skill_card_label_en", GetCardLabel(player.PersonSkillCardId, "eng"))
                .With("persona_skill_use_count", player.PersonSkillUseCount)
                .With("obtained_tokens", player.ObtainedTokens.ToArray())
                .With("obtained_token_labels", player.ObtainedTokens.Select(GetRelicLabel).ToArray())
                .With("obtained_token_labels_zhs", player.ObtainedTokens.Select(tokenId => GetRelicLabel(tokenId, "zhs")).ToArray())
                .With("obtained_token_labels_en", player.ObtainedTokens.Select(tokenId => GetRelicLabel(tokenId, "eng")).ToArray())
                .With("is_victory", payload.Run.IsVictory)
                .With("run_time", payload.Run.RunTime)
                .With("ascension", payload.Run.Ascension)
                .With("current_act_index", payload.Run.CurrentActIndex)
                .With("total_floor", payload.Run.TotalFloor)
                .With("net_mode", payload.Run.NetMode)
                .With("player_count", payload.Run.PlayerCount));
    }

    private static string? GetRelicLabel(string? relicId)
    {
        return GetLocalizedTitle("relics", relicId);
    }

    private static string? GetRelicLabel(string? relicId, string locale)
    {
        return GetLocalizedTitle("relics", relicId, locale);
    }

    private static string? GetCardLabel(string? cardId)
    {
        return GetLocalizedTitle("cards", cardId);
    }

    private static string? GetCardLabel(string? cardId, string locale)
    {
        return GetLocalizedTitle("cards", cardId, locale);
    }

    private static string? GetLocalizedTitle(string group, string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return null;

        var loc = LocString.GetIfExists(group, $"{modelId}.title");
        if (loc == null)
            return modelId;

        var text = loc.GetRawText();
        return string.IsNullOrWhiteSpace(text) ? modelId : text;
    }

    private static string? GetLocalizedTitle(string group, string? modelId, string locale)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return null;

        var normalizedLocale = NormalizeLocale(locale);
        if (string.Equals(normalizedLocale, NormalizeLocale(GetCurrentClientLocale()), StringComparison.OrdinalIgnoreCase))
            return GetLocalizedTitle(group, modelId);

        var table = GetLocalizationTable(group, normalizedLocale);
        if (table.TryGetValue($"{modelId}.title", out var text) && !string.IsNullOrWhiteSpace(text))
            return text;

        return modelId;
    }

    private static string GetCurrentClientLocale()
    {
        try
        {
            var locale = GodotTranslationServer.GetLocale();
            return string.IsNullOrWhiteSpace(locale) ? "unknown" : locale;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return "unknown";

        var normalized = locale.Trim().Replace('_', '-').ToLowerInvariant();
        if (normalized.StartsWith("zh"))
            return "zhs";
        if (normalized.StartsWith("en"))
            return "eng";
        if (normalized.StartsWith("ja") || normalized.StartsWith("jp") || normalized == "jpn")
            return "jpn";

        return normalized;
    }

    private static IReadOnlyDictionary<string, string> GetLocalizationTable(string group, string locale)
    {
        var normalizedLocale = NormalizeLocale(locale);
        return LocalizationTableCache.GetOrAdd($"{normalizedLocale}:{group}", static key =>
        {
            var separatorIndex = key.IndexOf(':');
            var resolvedLocale = separatorIndex >= 0 ? key[..separatorIndex] : "unknown";
            var resolvedGroup = separatorIndex >= 0 ? key[(separatorIndex + 1)..] : key;
            return LoadLocalizationTable(resolvedLocale, resolvedGroup);
        });
    }

    private static IReadOnlyDictionary<string, string> LoadLocalizationTable(string locale, string group)
    {
        try
        {
            var assemblyPath = typeof(MainFile).Assembly.Location;
            var modDirectory = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrWhiteSpace(modDirectory))
                return new Dictionary<string, string>(StringComparer.Ordinal);

            var path = Path.Combine(modDirectory, "ReAstralPartyMod", "localization", locale, $"{group}.json");
            if (!File.Exists(path))
                path = Path.Combine(Directory.GetParent(modDirectory)?.FullName ?? modDirectory, "ReAstralPartyMod", "localization", locale, $"{group}.json");
            if (!File.Exists(path))
                return new Dictionary<string, string>(StringComparer.Ordinal);

            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new Dictionary<string, string>(StringComparer.Ordinal);

            var dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                    dictionary[property.Name] = property.Value.GetString() ?? string.Empty;
            }

            return dictionary;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to load localization table locale={locale} group={group}: {ex.Message}");
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private static string? TryGetModManifestPath()
    {
        try
        {
            var assemblyPath = typeof(MainFile).Assembly.Location;
            var modDirectory = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrWhiteSpace(modDirectory))
                return null;

            var candidates = new[]
            {
                Path.Combine(modDirectory, "ReAstralPartyMod.json"),
                Path.Combine(Directory.GetParent(modDirectory)?.FullName ?? modDirectory, "ReAstralPartyMod.json")
            };

            return candidates.FirstOrDefault(File.Exists);
        }
        catch
        {
            return null;
        }
    }

    private static string Sha256Hex(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

internal static class AstralTelemetryDictionaryExtensions
{
    public static Dictionary<string, object?> With(this Dictionary<string, object?> dictionary, string key,
        object? value)
    {
        dictionary[key] = value;
        return dictionary;
    }
}

internal static class PersonSkillRegistry
{
    private static readonly Dictionary<string, string> PersonToSkillCardId = new(StringComparer.Ordinal)
    {
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_BLUE_WHALE"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FATE_WEAK_MPRINT",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_CYBER_KITTY"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_REMOTE_INTRUSION",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_DEITY_LIN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_LIVING_FOLIO",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_FENG"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_CHANNEL_ENERGY",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_INK_SHADOW_HUNTER"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_SHADOW_FUSION",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_JILL_STEINLE"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_MIXED_COCKTAILS",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_JUNK_BOT"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_COME_HERE_YOU",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_KAWAII_ANGEL"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_CYBER_ANGEL",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_MASCOT_GIRL_MIMI"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_PRODUCT_RESTOCKING",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_MIDNIGHT_FLASH"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_UNSTOPPABLE",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_MOUSY_LIAN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_SAVE_ME_MOUSY",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_NEEDY_GIRL"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_EMOTIONAL_OVERDOSE",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_NINJA"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_NINJUTSU_COMBO",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_OASIS_QUEEN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_ROYAL_PREROGATIVE",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_PANDA_MENG"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_BIG_EATER",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_POISONED_APPLE"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_CONCEALING_OPERATION",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_PROPRIETRESS"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_TRANSFER",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_SAMURAI_PRAWN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_FAMOUS_BLADE",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_SHADOW_SCION"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_POWERFUL_PITY",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_SLIME_LULU"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_HEALING_SLIME",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_SOCIAL_FEAR_NUN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_IRON_VIRGIN",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_SUPERMAN_MEGAS"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_SOLAR_BOMBARDMENT",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_UNCLE_PEDERMAN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_REALLY_ANGRY",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_VAMPIRE"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_VAMPIRE_BITE",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_WEIRD_EGG"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_TROUBLE_MAKER",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_XIAO_LEI"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_CHAIN_REACTION",
        ["RE_ASTRAL_PARTY_MOD_RELIC_PERSON_ZHAO"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_INVOKE_SPIRITS",
        ["RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_LING_YU_LIN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_SUMMON_RAIN",
        ["RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_LING_YULIN"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_SUMMON_RAIN",
        ["RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_SINKOU"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_PUNITIVE_JUDGMENT",
        ["RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_WEIRD_EGG"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_ANOMALY_MAKER"
    };

    private static readonly HashSet<string> TrackedPersonSkillCardIds =
        PersonToSkillCardId.Values.ToHashSet(StringComparer.Ordinal);

    public static string? TryGetPersonSkillCardId(string personRelicId)
    {
        return PersonToSkillCardId.GetValueOrDefault(personRelicId);
    }

    public static bool IsTrackedPersonSkillCard(CardModel canonicalCard)
    {
        return TrackedPersonSkillCardIds.Contains(canonicalCard.Id.Entry);
    }
}
