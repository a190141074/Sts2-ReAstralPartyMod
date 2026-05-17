using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class AstralTelemetry
{
    private enum TelemetryConsentState
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

    private sealed record TelemetryState(
        TelemetryConsentState ConsentState,
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
        IReadOnlyList<PersonaChoiceRecord> PersonaChoices,
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

    private sealed record PostHogEnvelope(
        [property: JsonPropertyName("api_key")]
        string ApiKey,
        [property: JsonPropertyName("batch")] IReadOnlyList<PostHogEvent> Batch);

    private sealed record PostHogEvent(
        [property: JsonPropertyName("event")] string Event,
        [property: JsonPropertyName("distinct_id")]
        string DistinctId,
        [property: JsonPropertyName("properties")]
        Dictionary<string, object?> Properties,
        [property: JsonPropertyName("timestamp")]
        string Timestamp);

    internal sealed record PersonaChoiceRecord(
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
        string? PersonaSelected,
        string? PersonaSkillCardId,
        int PersonaSkillUseCount,
        IReadOnlyList<string> ObtainedTokens);

    private sealed class PlayerTelemetryState
    {
        public required int Slot { get; init; }
        public required string Character { get; init; }
        public string? PersonaSelected { get; set; }
        public string? PersonaSkillCardId { get; set; }
        public int PersonaSkillUseCount { get; set; }
        public HashSet<string> ObtainedTokenIds { get; } = new(StringComparer.Ordinal);
    }

    private const string DefaultProvider = "posthog";
    private const string DefaultHost = "https://us.i.posthog.com";
    private const string DefaultProjectToken = "phc_tUUfTxDRKbzBcq24G78yPUu3w6TYkmCySG4cRwp7bJwC";
    private const string ConfigFileName = "telemetry_config.json";
    private const string LocalConfigFileName = "telemetry_config.local.json";
    private const string StateFileName = "telemetry_state.json";
    private const string PendingFileName = "telemetry_pending.jsonl";
    private const string ActiveRunFileName = "telemetry_active_run.json";
    private const string InstallSaltFileName = "telemetry_install_salt.txt";
    private const int MaxPendingLines = 64;
    private const long MinRunTimeForUploadSeconds = 180;
    private const string CaptureBatchPath = "/batch/";

    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly object QueueLock = new();
    private static readonly object StateLock = new();
    private static readonly object ConfigLock = new();
    private static readonly HashSet<string> SubmittedRunIds = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, RunTelemetryState> ActiveRuns = new(StringComparer.Ordinal);
    private static TelemetryConfig? _cachedConfig;
    private static TelemetryState? _cachedState;

    private sealed class RunTelemetryState
    {
        public string RunKey { get; init; } = string.Empty;
        public string Seed { get; init; } = string.Empty;
        public Dictionary<ulong, PlayerTelemetryState> PlayerStates { get; } = new();
        public List<PersonaChoiceRecord> PersonaChoices { get; } = [];
        public List<TokenChoiceRecord> TokenChoices { get; } = [];
    }

    private sealed record ActiveRunSnapshot(
        string RunKey,
        string Seed,
        IReadOnlyList<PlayerTelemetrySnapshot> Players,
        IReadOnlyList<PersonaChoiceRecord> PersonaChoices,
        IReadOnlyList<TokenChoiceRecord> TokenChoices);

    private sealed record PlayerTelemetrySnapshot(
        ulong NetId,
        int Slot,
        string Character,
        string? PersonaSelected,
        string? PersonaSkillCardId,
        int PersonaSkillUseCount,
        IReadOnlyList<string> ObtainedTokens);

    public static void Initialize()
    {
        try
        {
            EnsureConfigFile();
            EnsureStateFile();
            SyncFromModSettings();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Config init failed: {ex.Message}");
        }
    }

    public static void SyncFromModSettings()
    {
        try
        {
            var enabled = ReAstralPartyModSettingsManager.EnableTelemetry;
            lock (ConfigLock)
            {
                var state = LoadStateCore();
                if (state.ConsentState == TelemetryConsentState.Unknown)
                {
                    SaveStateCore(state with
                    {
                        ConsentState = enabled ? TelemetryConsentState.Accepted : TelemetryConsentState.Declined,
                        ConsentPromptShown = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to sync telemetry setting from mod settings: {ex.Message}");
        }
    }

    public static bool ShouldShowConsentPrompt()
    {
        var config = LoadConfig();
        if (!config.Enabled)
            return false;

        var state = LoadState();
        return state.ConsentState == TelemetryConsentState.Unknown;
    }

    public static void SetCollectionEnabledByConsent(bool enabled)
    {
        lock (ConfigLock)
        {
            var state = LoadStateCore();
            SaveStateCore(state with
            {
                ConsentState = enabled ? TelemetryConsentState.Accepted : TelemetryConsentState.Declined,
                ConsentPromptShown = true
            });
        }

        MainFile.Logger.Info(
            $"[{MainFile.ModId}][Telemetry] User consent updated: enabled={enabled}");
    }

    public static bool IsCollectionEnabled()
    {
        return IsTelemetryEnabled(LoadConfig(), LoadState());
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
                    $"[{MainFile.ModId}][Telemetry] Restored active run snapshot runKey={runKey} personaChoices={state.PersonaChoices.Count} tokenChoices={state.TokenChoices.Count}.");
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
            state.PersonaChoices.Clear();
            foreach (var player in runState.Players.OrderBy(static player => player.NetId))
            {
                var slot = GetPlayerSlot(runState, player);
                var optionIds = options.Select(GetRelicId).ToArray();
                string? selected = null;
                if (selectedIndexes.TryGetValue(player.NetId, out var selectedIndex)
                    && selectedIndex >= 0
                    && selectedIndex < options.Count)
                    selected = GetRelicId(options[selectedIndex]);

                state.PersonaChoices.Add(new PersonaChoiceRecord(slot, optionIds, selected));
                if (state.PlayerStates.TryGetValue(player.NetId, out var playerState))
                {
                    playerState.PersonaSelected = selected;
                    playerState.PersonaSkillCardId =
                        selected == null ? null : PersonaSkillRegistry.TryGetPersonaSkillCardId(selected);
                }
            }

            MainFile.Logger.Info(
                $"[{MainFile.ModId}][Telemetry] Recorded persona choices: players={state.PersonaChoices.Count} runKey={state.RunKey}");
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
        if (!PersonaSkillRegistry.IsTrackedPersonaSkillCard(canonicalCard))
            return;

        var state = GetOrCreateRunState(runState);
        lock (StateLock)
        {
            if (!state.PlayerStates.TryGetValue(card.Owner.NetId, out var playerState))
                return;

            if (playerState.PersonaSkillCardId == null)
                playerState.PersonaSkillCardId = canonicalCard.Id.Entry;

            if (!string.Equals(playerState.PersonaSkillCardId, canonicalCard.Id.Entry, StringComparison.Ordinal))
                return;

            playerState.PersonaSkillUseCount++;
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
            var state = LoadState();
            if (!IsTelemetryEnabled(config, state))
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

            var payload = BuildPayload(runState, serializableRun, isVictory, gameType);
            if (payload == null)
                return;

            if (!SubmittedRunIds.Add(payload.Run.RunId))
                return;

            var json = JsonSerializer.Serialize(payload, JsonOptions);
            _ = Task.Run(() => UploadPendingThenCurrentAsync(config, json, payload.Run.RunId));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Upload scheduling failed: {ex.Message}");
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
                    playerState?.PersonaSelected,
                    playerState?.PersonaSkillCardId,
                    playerState?.PersonaSkillUseCount ?? 0,
                    playerState?.ObtainedTokenIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray() ?? []);
            })
            .ToArray();

        MainFile.Logger.Info(
            $"[{MainFile.ModId}][Telemetry] Built payload: personaChoices={state.PersonaChoices.Count} tokenChoices={state.TokenChoices.Count} players={players.Length} runKey={state.RunKey}");

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
            state.PersonaChoices.ToArray(),
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
                pair.Value.PersonaSelected,
                pair.Value.PersonaSkillCardId,
                pair.Value.PersonaSkillUseCount,
                pair.Value.ObtainedTokenIds.OrderBy(static id => id, StringComparer.Ordinal).ToArray()))
            .ToArray();

        return new ActiveRunSnapshot(
            state.RunKey,
            state.Seed,
            players,
            state.PersonaChoices.ToArray(),
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
        state.PersonaChoices.Clear();
        state.PersonaChoices.AddRange(snapshot.PersonaChoices ?? []);
        state.TokenChoices.Clear();
        state.TokenChoices.AddRange(snapshot.TokenChoices ?? []);

        foreach (var player in snapshot.Players ?? [])
        {
            if (!state.PlayerStates.TryGetValue(player.NetId, out var playerState))
                continue;

            playerState.PersonaSelected = player.PersonaSelected;
            playerState.PersonaSkillCardId = player.PersonaSkillCardId;
            playerState.PersonaSkillUseCount = Math.Max(0, player.PersonaSkillUseCount);
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

    private static async Task UploadPendingThenCurrentAsync(TelemetryConfig config, string currentJson, string runId)
    {
        var pending = ReadPendingPayloads();
        pending.Add(currentJson);

        var unsent = new List<string>();
        foreach (var payload in pending.TakeLast(MaxPendingLines))
        {
            if (IsShortRunPayload(payload))
                continue;

            try
            {
                var runPayload = JsonSerializer.Deserialize<RunEndedPayload>(payload, JsonOptions);
                if (runPayload == null)
                    continue;

                var envelope = BuildPostHogEnvelope(config, runPayload);
                if (envelope.Batch.Count == 0)
                    continue;

                var contentJson = JsonSerializer.Serialize(envelope, JsonOptions);
                using var content = new StringContent(contentJson, Encoding.UTF8, "application/json");
                using var response = await HttpClient.PostAsync(BuildBatchEndpoint(config.Host), content);
                if (!response.IsSuccessStatusCode)
                    unsent.Add(payload);
            }
            catch
            {
                unsent.Add(payload);
            }
        }

        WritePendingPayloads(unsent.TakeLast(MaxPendingLines).ToList());
        if (unsent.Count == 0)
            MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Uploaded run={runId}");
        else
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Upload deferred unsent={unsent.Count}");
    }

    private static bool IsShortRunPayload(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("run", out var run)
                && run.TryGetProperty("runTime", out var runTimeElement)
                && runTimeElement.TryGetInt64(out var runTime))
                return runTime <= MinRunTimeForUploadSeconds;
        }
        catch
        {
            return false;
        }

        return false;
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
            var config = new TelemetryConfig(true, DefaultProvider, DefaultHost, DefaultProjectToken, PendingFileName);
            WriteConfigFile(config);
        }
    }

    private static void EnsureStateFile()
    {
        var statePath = GetStatePath();
        Directory.CreateDirectory(GetDataDirectory());
        if (File.Exists(statePath))
            return;

        SaveStateCore(new TelemetryState(TelemetryConsentState.Unknown, false));
    }

    private static List<string> ReadPendingPayloads()
    {
        lock (QueueLock)
        {
            var path = GetPendingPath();
            if (!File.Exists(path))
                return [];

            return File.ReadLines(path)
                .Where(static line => !string.IsNullOrWhiteSpace(line))
                .TakeLast(MaxPendingLines)
                .ToList();
        }
    }

    private static void WritePendingPayloads(IReadOnlyList<string> payloads)
    {
        lock (QueueLock)
        {
            var path = GetPendingPath();
            Directory.CreateDirectory(GetDataDirectory());
            if (payloads.Count == 0)
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }

            File.WriteAllLines(path, payloads);
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

    private static string GetPendingPath()
    {
        var pendingFile = LoadConfig().PendingFile;
        return Path.Combine(GetDataDirectory(), string.IsNullOrWhiteSpace(pendingFile) ? PendingFileName : pendingFile);
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
                    PendingFileName);
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

        _cachedConfig = new TelemetryConfig(true, DefaultProvider, DefaultHost, DefaultProjectToken, PendingFileName);
        return _cachedConfig;
    }

    private static TelemetryState LoadState()
    {
        lock (ConfigLock)
        {
            return LoadStateCore();
        }
    }

    private static TelemetryState LoadStateCore()
    {
        if (_cachedState != null)
            return _cachedState;

        EnsureStateFile();
        try
        {
            var json = File.ReadAllText(GetStatePath());
            var state = JsonSerializer.Deserialize<TelemetryState>(json, JsonOptions);
            if (state != null)
            {
                _cachedState = state;
                return state;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] State read failed: {ex.Message}");
        }

        _cachedState = new TelemetryState(TelemetryConsentState.Unknown, false);
        return _cachedState;
    }

    private static void SaveStateCore(TelemetryState state)
    {
        Directory.CreateDirectory(GetDataDirectory());
        File.WriteAllText(
            GetStatePath(),
            JsonSerializer.Serialize(state, new JsonSerializerOptions(JsonOptions) { WriteIndented = true }));
        _cachedState = state;
    }

    private static bool IsTelemetryEnabled(TelemetryConfig config, TelemetryState state)
    {
        return config.Enabled && state.ConsentState == TelemetryConsentState.Accepted;
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
            PendingFile = string.IsNullOrWhiteSpace(config.PendingFile) ? PendingFileName : config.PendingFile
        };
    }

    private static string BuildBatchEndpoint(string host)
    {
        return $"{host.TrimEnd('/')}{CaptureBatchPath}";
    }

    private static PostHogEnvelope BuildPostHogEnvelope(TelemetryConfig config, RunEndedPayload payload)
    {
        var events = new List<PostHogEvent>();
        foreach (var choice in payload.PersonaChoices)
        {
            events.AddRange(CreatePersonaOptionOfferedEvents(payload, choice));
            events.Add(CreatePersonaChoiceEvent(payload, choice));
        }

        foreach (var choice in payload.TokenChoices)
        {
            events.AddRange(CreateTokenOptionOfferedEvents(payload, choice));
            events.Add(CreateTokenChoiceEvent(payload, choice));
        }

        foreach (var player in payload.Players)
        {
            foreach (var tokenId in player.ObtainedTokens)
                events.Add(CreateTokenObtainedEvent(payload, player, tokenId));

            events.Add(CreateRunFinishedEvent(payload, player));
        }

        return new PostHogEnvelope(config.ProjectToken, events);
    }

    private static IEnumerable<PostHogEvent> CreatePersonaOptionOfferedEvents(RunEndedPayload payload,
        PersonaChoiceRecord choice)
    {
        foreach (var optionId in choice.Options)
            yield return new PostHogEvent(
                "astral_persona_option_offered",
                CreateDistinctId(payload.Run.RunId, choice.PlayerSlot),
                CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                    .With("option_id", optionId)
                    .With("option_label", GetRelicLabel(optionId))
                    .With("selected", choice.Selected)
                    .With("selected_label", GetRelicLabel(choice.Selected))
                    .With("is_selected", string.Equals(optionId, choice.Selected, StringComparison.Ordinal)),
                payload.UploadedAtUtc);
    }

    private static IEnumerable<PostHogEvent> CreateTokenOptionOfferedEvents(RunEndedPayload payload,
        TokenChoiceRecord choice)
    {
        foreach (var optionId in choice.Options)
            yield return new PostHogEvent(
                "astral_token_option_offered",
                CreateDistinctId(payload.Run.RunId, choice.PlayerSlot),
                CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                    .With("source", choice.Source)
                    .With("option_id", optionId)
                    .With("option_label", GetRelicLabel(optionId))
                    .With("selected", choice.Selected)
                    .With("selected_label", GetRelicLabel(choice.Selected))
                    .With("is_selected", string.Equals(optionId, choice.Selected, StringComparison.Ordinal))
                    .With("reroll_count", choice.RerollCount),
                payload.UploadedAtUtc);
    }

    private static PostHogEvent CreatePersonaChoiceEvent(RunEndedPayload payload, PersonaChoiceRecord choice)
    {
        var optionLabels = choice.Options.Select(GetRelicLabel).ToArray();
        return new PostHogEvent(
            "astral_persona_choice",
            CreateDistinctId(payload.Run.RunId, choice.PlayerSlot),
            CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                .With("options", choice.Options.ToArray())
                .With("option_labels", optionLabels)
                .With("selected", choice.Selected)
                .With("selected_label", GetRelicLabel(choice.Selected)),
            payload.UploadedAtUtc);
    }

    private static PostHogEvent CreateTokenChoiceEvent(RunEndedPayload payload, TokenChoiceRecord choice)
    {
        var optionLabels = choice.Options.Select(GetRelicLabel).ToArray();
        return new PostHogEvent(
            "astral_token_choice",
            CreateDistinctId(payload.Run.RunId, choice.PlayerSlot),
            CreateBaseProperties(payload, choice.PlayerSlot, null, null)
                .With("source", choice.Source)
                .With("options", choice.Options.ToArray())
                .With("option_labels", optionLabels)
                .With("selected", choice.Selected)
                .With("selected_label", GetRelicLabel(choice.Selected))
                .With("reroll_count", choice.RerollCount),
            payload.UploadedAtUtc);
    }

    private static PostHogEvent CreateTokenObtainedEvent(RunEndedPayload payload, PlayerRunTelemetry player,
        string tokenId)
    {
        return new PostHogEvent(
            "astral_token_obtained",
            CreateDistinctId(payload.Run.RunId, player.Slot),
            CreateBaseProperties(payload, player.Slot, player, player.PersonaSelected)
                .With("token_id", tokenId)
                .With("token_label", GetRelicLabel(tokenId))
                .With("is_victory", payload.Run.IsVictory)
                .With("run_time", payload.Run.RunTime)
                .With("ascension", payload.Run.Ascension)
                .With("current_act_index", payload.Run.CurrentActIndex)
                .With("total_floor", payload.Run.TotalFloor)
                .With("net_mode", payload.Run.NetMode)
                .With("player_count", payload.Run.PlayerCount),
            payload.UploadedAtUtc);
    }

    private static PostHogEvent CreateRunFinishedEvent(RunEndedPayload payload, PlayerRunTelemetry player)
    {
        return new PostHogEvent(
            "astral_run_finished",
            CreateDistinctId(payload.Run.RunId, player.Slot),
            CreateBaseProperties(payload, player.Slot, player, player.PersonaSelected)
                .With("character", player.Character)
                .With("persona_selected", player.PersonaSelected)
                .With("persona_selected_label", GetRelicLabel(player.PersonaSelected))
                .With("persona_skill_card_id", player.PersonaSkillCardId)
                .With("persona_skill_card_label", GetCardLabel(player.PersonaSkillCardId))
                .With("persona_skill_use_count", player.PersonaSkillUseCount)
                .With("obtained_tokens", player.ObtainedTokens.ToArray())
                .With("obtained_token_labels", player.ObtainedTokens.Select(GetRelicLabel).ToArray())
                .With("is_victory", payload.Run.IsVictory)
                .With("run_time", payload.Run.RunTime)
                .With("ascension", payload.Run.Ascension)
                .With("current_act_index", payload.Run.CurrentActIndex)
                .With("total_floor", payload.Run.TotalFloor)
                .With("net_mode", payload.Run.NetMode)
                .With("player_count", payload.Run.PlayerCount),
            payload.UploadedAtUtc);
    }

    private static Dictionary<string, object?> CreateBaseProperties(
        RunEndedPayload payload,
        int playerSlot,
        PlayerRunTelemetry? player,
        string? personaSelected)
    {
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
            ["character"] = player?.Character,
            ["persona_selected"] = personaSelected,
            ["persona_selected_label"] = GetRelicLabel(personaSelected)
        };
    }

    private static string CreateDistinctId(string runId, int playerSlot)
    {
        return Sha256Hex($"{runId}|{playerSlot}|{GetInstallSalt()}");
    }

    private static string? GetRelicLabel(string? relicId)
    {
        return GetLocalizedTitle("relics", relicId);
    }

    private static string? GetCardLabel(string? cardId)
    {
        return GetLocalizedTitle("cards", cardId);
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

    private static string GetInstallSalt()
    {
        var saltPath = Path.Combine(GetDataDirectory(), InstallSaltFileName);
        try
        {
            if (File.Exists(saltPath))
                return File.ReadAllText(saltPath).Trim();

            Directory.CreateDirectory(GetDataDirectory());
            var salt = Guid.NewGuid().ToString("N");
            File.WriteAllText(saltPath, salt);
            return salt;
        }
        catch
        {
            return MainFile.ModId;
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

internal static class PersonaSkillRegistry
{
    private static readonly Dictionary<string, string> PersonaToSkillCardId = new(StringComparer.Ordinal)
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
        ["RE_ASTRAL_PARTY_MOD_RELIC_VARIANT_PERSON_WEIRD_EGG"] = "RE_ASTRAL_PARTY_MOD_CARD_SKILL_ANOMALY_MAKER"
    };

    private static readonly HashSet<string> TrackedPersonaSkillCardIds =
        PersonaToSkillCardId.Values.ToHashSet(StringComparer.Ordinal);

    public static string? TryGetPersonaSkillCardId(string personaRelicId)
    {
        return PersonaToSkillCardId.GetValueOrDefault(personaRelicId);
    }

    public static bool IsTrackedPersonaSkillCard(CardModel canonicalCard)
    {
        return TrackedPersonaSkillCardIds.Contains(canonicalCard.Id.Entry);
    }
}
