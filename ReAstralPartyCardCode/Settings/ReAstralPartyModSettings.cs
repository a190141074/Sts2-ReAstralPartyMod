using Godot;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public enum TokenSeriesMode
{
    RandomTwo = 0,
    All = 1,
    Disabled = 2
}

public enum NeowExtraOptionSelectionMode
{
    DefaultRandom = 0,
    DreamFaceTheShadow = 1,
    RingOfSevenCurses = 2,
    AbsoluteForm = 3,
    ProphecySoulDevour = 4,
    ProphecyReplicantGroup = 5,
    DreamCoinExplosion = 6,
    DreamDisintegrationClaw = 7,
    TetraWarforge = 8
}

public enum StartingPersonaDisplayMode
{
    Manual = 0,
    Automatic = 1
}

public enum StartingPersonaAssignmentMode
{
    Independent = 0,
    Clone = 1
}

public enum StartingPersonaMode
{
    Standard = 0,
    StandardDuplicate = 1,
    RandomAssign = 2,
    Clone = 3,
    RandomClone = 4,
    DestinedClone = 5
}

public sealed class ReAstralPartyModSettings
{
    // Settings defaults are initialized before content registration, so banned relic defaults
    // must not resolve ids from ModelDb here.
    internal static readonly string[] DefaultBannedRelicIds =
    [
        "RELIC.RE_ASTRAL_PARTY_MOD_RELIC_MOON_PROP_BEADS_OF_FEALTY"
    ];

    internal const string LegacyMoonPropBeadsOfFealtyRelicId = "RELIC.MOON_PROP_BEADS_OF_FEALTY";

    public bool DefaultBannedRelicsInitialized { get; set; }

    public sealed class AstralModeScopedGameplaySettings
    {
        public bool EnableExtremeMode { get; set; }

        public bool EnableStartingInitialPoint { get; set; }

        public bool EnableStartingAstralRelicStore { get; set; } = true;

        public bool EnableStartingRingOfSevenCurses { get; set; }

        public bool EnableStartingPersonaSelection { get; set; } = true;

        public bool EnableDreamSeriesEvents { get; set; } = true;

        public bool EnableEnigmaticSeriesEvents { get; set; } = true;

        public bool EnableMoonPropShopSlots { get; set; } = true;

        public bool EnableMoonPropRelics { get; set; } = true;

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

        public AstralModeScopedGameplaySettings Clone()
        {
            return new AstralModeScopedGameplaySettings
            {
                EnableExtremeMode = EnableExtremeMode,
                EnableStartingInitialPoint = EnableStartingInitialPoint,
                EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
                EnableStartingRingOfSevenCurses = EnableStartingRingOfSevenCurses,
                EnableStartingPersonaSelection = EnableStartingPersonaSelection,
                EnableDreamSeriesEvents = EnableDreamSeriesEvents,
                EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
                EnableMoonPropShopSlots = EnableMoonPropShopSlots,
                EnableMoonPropRelics = EnableMoonPropRelics,
                EnableNeowExtraOption = EnableNeowExtraOption,
                EnableLucidDream = EnableLucidDream,
                EnableCollectorsCards = EnableCollectorsCards,
                NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
                EnableAllPersonas = EnableAllPersonas,
                EnableVariantPersonas = EnableVariantPersonas,
                EnableAllVariantPersonas = EnableAllVariantPersonas,
                StartingPersonaMode = StartingPersonaMode,
                TokenSeriesMode = TokenSeriesMode
            };
        }
    }

    public sealed class LobbyPanelStateModel
    {
        public bool IsCollapsed { get; set; }

        public float PositionX { get; set; } = 1140f;

        public float PositionY { get; set; } = 120f;

        public float Width { get; set; } = 440f;

        public float Height { get; set; } = 520f;
    }

    public List<string> BannedRelicIds { get; set; } = [.. DefaultBannedRelicIds];

    // Legacy field kept so older settings.json files still load cleanly.
    public List<string>? BannedPersonaRelicIds { get; set; } = [.. DefaultBannedRelicIds];

    public AstralContentMode CurrentContentMode { get; set; } = AstralContentMode.Vanilla;

    public bool ContentModeSettingsInitialized { get; set; }

    public AstralModeScopedGameplaySettings VanillaModeSettings { get; set; } = new();

    public AstralModeScopedGameplaySettings ModpackModeSettings { get; set; } = new();

    public bool EnableExtremeMode { get; set; }

    public bool EnableStartingInitialPoint { get; set; }

    public bool EnableStartingAstralRelicStore { get; set; } = true;

    public bool EnableStartingRingOfSevenCurses { get; set; }

    public bool EnableStartingPersonaSelection { get; set; } = true;

    public bool EnableDreamSeriesEvents { get; set; } = true;

    public bool EnableEnigmaticSeriesEvents { get; set; } = true;

    public bool EnableMoonPropShopSlots { get; set; } = true;

    public bool EnableMoonPropRelics { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;

    public bool EnableLucidDream { get; set; } = true;

    public bool EnableCollectorsCards { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersonas { get; set; }

    public bool EnableVariantPersonas { get; set; } = true;

    public bool EnableAllVariantPersonas { get; set; }

    // Legacy fields kept so older settings.json files still load cleanly.
    public bool? EnableDuplicatePersonas { get; set; }
    public bool? EnableRandomCloneMode { get; set; }

    public StartingPersonaDisplayMode? StartingPersonaDisplayMode { get; set; }

    public StartingPersonaAssignmentMode? StartingPersonaAssignmentMode { get; set; }

    public StartingPersonaMode StartingPersonaMode { get; set; } = StartingPersonaMode.Standard;

    public bool EnablePlayRecommendation { get; set; }

    public bool EnableRouteRecommendation { get; set; }

    public bool EnableTokenRecommendation { get; set; }

    public bool EnableAutoPhrase { get; set; }

    public bool EnableTelemetry { get; set; } = true;

    public bool EnableStartupNotifications { get; set; } = true;

    public bool EnableSettingsNotifications { get; set; } = true;

    public bool EnableTelemetryNotifications { get; set; } = true;

    public bool EnableMultiplayerNotifications { get; set; } = true;

    public bool EnableConsoleCommandNotifications { get; set; } = true;

    public bool EnablePersonaRelicNotifications { get; set; } = true;

    public bool EnableTokenRelicNotifications { get; set; } = true;

    public bool EnableNeowDiagnosticsNotifications { get; set; } = true;

    // Legacy bool setting kept for backward compatibility with older settings.json files.
    public bool? EnableAllTokenSeries { get; set; }

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;

    public LobbyPanelStateModel LobbyPanelState { get; set; } = new();
}

public static partial class ReAstralPartyModSettingsManager
{
    public const string SettingsKey = "settings";
    private const string RunSettingsSnapshotKey = "gameplay_run_settings_snapshot";
    private static readonly Regex CamelCaseRegex = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);

    private static readonly object RuntimeSettingsGate = new();
    private static RunSavedData<ReAstralPartyRunSettingsSnapshot> _runSettingsSnapshots = null!;

    private static LocalRuntimeSettings _runtimeSettings = LocalRuntimeSettings.FromPersistent(
        new ReAstralPartyModSettings());

    private static bool _registered;
    private static bool _loggedMissingGameplaySnapshot;

    public static bool EnableAllPersonas => ReadRuntime(settings => settings.EnableAllPersonas);

    public static bool EnableVariantPersonas => ReadRuntime(settings => settings.EnableVariantPersonas);

    public static bool EnableAllVariantPersonas => ReadRuntime(settings => settings.EnableAllVariantPersonas);

    public static bool EnableExtremeMode => ReadRuntime(settings => settings.EnableExtremeMode);

    public static bool EnableStartingInitialPoint => ReadRuntime(settings => settings.EnableStartingInitialPoint);

    public static bool EnableStartingAstralRelicStore => ReadRuntime(settings => settings.EnableStartingAstralRelicStore);

    public static bool EnableStartingRingOfSevenCurses => ReadRuntime(settings => settings.EnableStartingRingOfSevenCurses);

    public static bool EnableStartingPersonaSelection => ReadRuntime(settings => settings.EnableStartingPersonaSelection);

    public static bool EnableDreamSeriesEvents => ReadRuntime(settings => settings.EnableDreamSeriesEvents);

    public static bool EnableEnigmaticSeriesEvents => ReadRuntime(settings => settings.EnableEnigmaticSeriesEvents);

    public static bool EnableMoonPropShopSlots => ReadRuntime(settings => settings.EnableMoonPropShopSlots);

    public static bool EnableMoonPropRelics => ReadRuntime(settings => settings.EnableMoonPropRelics);

    public static bool EnableNeowExtraOption => ReadRuntime(settings => settings.EnableNeowExtraOption);

    public static bool EnableLucidDream => ReadRuntime(settings => settings.EnableLucidDream);

    public static bool EnableCollectorsCards => ReadRuntime(settings => settings.EnableCollectorsCards);

    public static NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode =>
        ReadRuntime(settings => settings.NeowExtraOptionSelectionMode);

    public static StartingPersonaMode ConfiguredStartingPersonaMode =>
        ReadRuntime(settings => settings.StartingPersonaMode);

    public static TokenSeriesMode TokenSeriesMode => ReadRuntime(settings => settings.TokenSeriesMode);

    public static bool EnablePureAngelMode => ReadRuntime(settings => settings.EnablePureAngelMode);

    public static ReAstralPartyModSettings.LobbyPanelStateModel LobbyPanelState =>
        ReadRuntime(settings => new ReAstralPartyModSettings.LobbyPanelStateModel
        {
            IsCollapsed = settings.LobbyPanelState.IsCollapsed,
            PositionX = settings.LobbyPanelState.PositionX,
            PositionY = settings.LobbyPanelState.PositionY,
            Width = settings.LobbyPanelState.Width,
            Height = settings.LobbyPanelState.Height
        });

    public static bool EnablePlayRecommendation => ReadRuntime(settings => settings.EnablePlayRecommendation);

    public static bool EnableRouteRecommendation => ReadRuntime(settings => settings.EnableRouteRecommendation);

    public static bool EnableTokenRecommendation => ReadRuntime(settings => settings.EnableTokenRecommendation);

    public static bool EnableAutoPhrase => ReadRuntime(settings => settings.EnableAutoPhrase);

    public static bool EnableTelemetry => ReadRuntime(settings => settings.EnableTelemetry);

    public static bool EnableStartupNotifications => ReadRuntime(settings => settings.EnableStartupNotifications);

    public static bool EnableSettingsNotifications => ReadRuntime(settings => settings.EnableSettingsNotifications);

    public static bool EnableTelemetryNotifications => ReadRuntime(settings => settings.EnableTelemetryNotifications);

    public static bool EnableMultiplayerNotifications =>
        ReadRuntime(settings => settings.EnableMultiplayerNotifications);

    public static bool EnableConsoleCommandNotifications =>
        ReadRuntime(settings => settings.EnableConsoleCommandNotifications);

    public static bool EnablePersonaRelicNotifications =>
        ReadRuntime(settings => settings.EnablePersonaRelicNotifications);

    public static bool EnableTokenRelicNotifications =>
        ReadRuntime(settings => settings.EnableTokenRelicNotifications);

    public static bool EnableNeowDiagnosticsNotifications =>
        ReadRuntime(settings => settings.EnableNeowDiagnosticsNotifications);

    public static IReadOnlySet<ModelId> BannedRelicIds =>
        ReadRuntime(settings => settings.BannedRelicIds);

    public static IReadOnlySet<ModelId> BannedPersonaRelicIds => BannedRelicIds;

    public static bool EnableHiddenBetaCardPortraitMode => ReadRuntime(settings =>
        settings.EnablePlayRecommendation
        && settings.EnableRouteRecommendation
        && settings.EnableTokenRecommendation
        && settings.EnableAutoPhrase
        && !settings.EnablePureAngelMode);

    public static ReAstralPartyModSettings ReadLocalSettings()
    {
        try
        {
            var settings = RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey);
            EnsureContentModeSettingsInitialized(settings);
            return settings;
        }
        catch
        {
            var settings = new ReAstralPartyModSettings();
            EnsureContentModeSettingsInitialized(settings);
            return settings;
        }
    }

    public static AstralContentMode GetCurrentContentMode()
    {
        return ReadRuntime(settings => settings.CurrentContentMode);
    }

    public static AstralContentMode GetCurrentContentMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var runSnapshot))
            return AstralContentModeRegistry.NormalizeMode(runSnapshot.CurrentContentMode);

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return AstralContentModeRegistry.NormalizeMode(lobbySnapshot.CurrentContentMode);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.CurrentContentMode;

        return GetCurrentContentMode();
    }

    public static void SetCurrentContentMode(AstralContentMode mode)
    {
        var normalizedMode = AstralContentModeRegistry.NormalizeMode(mode);
        UpdatePersistentSettings(settings => settings.CurrentContentMode = normalizedMode, "set_current_content_mode");
    }

    public static TokenSeriesMode ResolveTokenSeriesMode(ReAstralPartyModSettings settings)
    {
        return ResolveTokenSeriesModeCore(settings);
    }

    public static bool TryGetRunSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        if (TryGetStoredRunSettingsSnapshot(runState, out snapshot))
            return true;

        return ReAstralPartyRunSettingsSync.TryGetSnapshot(runState, out snapshot);
    }

    public static bool GetEnableAllPersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableAllPersonas;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableAllPersonas;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableAllPersonas;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableAllPersonas;
    }

    public static bool GetEnableVariantPersonas(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.VariantPersonas))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableVariantPersonas;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableVariantPersonas;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableVariantPersonas;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableVariantPersonas;
    }

    public static bool GetEnableDreamSeriesEvents(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.DreamSeriesEvents))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableDreamSeriesEvents;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableDreamSeriesEvents;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableDreamSeriesEvents;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableDreamSeriesEvents;
    }

    public static bool GetEnableEnigmaticSeriesEvents(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.EnigmaticSeriesEvents))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableEnigmaticSeriesEvents;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableEnigmaticSeriesEvents;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableEnigmaticSeriesEvents;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableEnigmaticSeriesEvents;
    }

    public static bool GetEnableMoonPropShopSlots(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.MoonPropShopSlots))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableMoonPropShopSlots;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableMoonPropShopSlots;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableMoonPropShopSlots;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableMoonPropShopSlots;
    }

    public static bool GetEnableMoonPropRelics(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.MoonPropRelics))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableMoonPropRelics;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableMoonPropRelics;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableMoonPropRelics;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableMoonPropRelics;
    }

    public static bool GetEnableNeowExtraOption(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.NeowExtraOption))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableNeowExtraOption;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableNeowExtraOption;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableNeowExtraOption;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableNeowExtraOption;
    }

    public static bool GetEnableLucidDream(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.LucidDream))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDream;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDream;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDream;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableLucidDream;
    }

    public static bool GetEnableCollectorsCards(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.CollectorsCards))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableCollectorsCards;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableCollectorsCards;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableCollectorsCards;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableCollectorsCards;
    }

    public static NeowExtraOptionSelectionMode GetNeowExtraOptionSelectionMode(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.NeowExtraOptionSelectionMode))
            return NeowExtraOptionSelectionMode.DefaultRandom;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return NormalizeNeowExtraOptionSelectionMode(
                snapshot.EnableStartingRingOfSevenCurses,
                snapshot.NeowExtraOptionSelectionMode);

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return NormalizeNeowExtraOptionSelectionMode(
                lobbySnapshot.EnableStartingRingOfSevenCurses,
                lobbySnapshot.NeowExtraOptionSelectionMode);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return NormalizeNeowExtraOptionSelectionMode(
                localFallback.EnableStartingRingOfSevenCurses,
                localFallback.NeowExtraOptionSelectionMode);

        if (ShouldUseSafeGameplayFallback(runState))
            return NeowExtraOptionSelectionMode.DefaultRandom;

        return NormalizeNeowExtraOptionSelectionMode(
            EnableStartingRingOfSevenCurses,
            NeowExtraOptionSelectionMode);
    }

    public static bool GetEnableStartingInitialPoint(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableStartingInitialPoint;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableStartingInitialPoint;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableStartingInitialPoint;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableStartingInitialPoint;
    }

    public static bool GetEnableStartingAstralRelicStore(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableStartingAstralRelicStore;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableStartingAstralRelicStore;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableStartingAstralRelicStore;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableStartingAstralRelicStore;
    }


    public static bool GetEnableStartingRingOfSevenCurses(IRunState? runState)
    {
        if (!IsGameplaySettingEnabledForMode(runState, AstralGameplaySettingKey.StartingRingOfSevenCurses))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableStartingRingOfSevenCurses;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableStartingRingOfSevenCurses;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableStartingRingOfSevenCurses;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableStartingRingOfSevenCurses;
    }

    public static bool GetEnableStartingPersonaSelection(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableStartingPersonaSelection;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableStartingPersonaSelection;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableStartingPersonaSelection;

        if (ShouldUseSafeGameplayFallback(runState))
            return true;

        return EnableStartingPersonaSelection;
    }

    public static bool GetEnableAllVariantPersonas(IRunState? runState)
    {
        if (!GetEnableVariantPersonas(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableAllVariantPersonas;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableAllVariantPersonas;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableAllVariantPersonas;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableAllVariantPersonas;
    }

    public static bool GetEnableExtremeMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableExtremeMode;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableExtremeMode;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableExtremeMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableExtremeMode;
    }

    public static bool GetEnableLucidDreamFalseLifeline(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamFalseLifeline;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamFalseLifeline;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamFalseLifeline;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamSmoothSailing(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamSmoothSailing;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamSmoothSailing;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamSmoothSailing;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamFishScalesMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamFishScalesMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamFishScalesMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamFishScalesMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamSevereWoundOneMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamSevereWoundOneMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamSevereWoundOneMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamSevereWoundOneMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamSevereWoundTwoMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamSevereWoundTwoMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamSevereWoundTwoMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamSevereWoundTwoMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamMadLifeMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamMadLifeMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamMadLifeMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamMadLifeMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamSwampOfFateMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamSwampOfFateMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamSwampOfFateMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamSwampOfFateMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamOverpopulationMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamOverpopulationMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamOverpopulationMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamOverpopulationMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamCautiousJellyfishMalice(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamCautiousJellyfishMalice;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamCautiousJellyfishMalice;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamCautiousJellyfishMalice;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamFaceDeathWithComposure(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamFaceDeathWithComposure;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamFaceDeathWithComposure;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamFaceDeathWithComposure;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamWildness(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamWildness;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamWildness;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamWildness;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamWildnessPhantom(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamWildnessPhantom;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamWildnessPhantom;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamWildnessPhantom;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamPitchBlackImpulse(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamPitchBlackImpulse;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamPitchBlackImpulse;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamPitchBlackImpulse;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamBubblePotionOfDreams(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamBubblePotionOfDreams;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamBubblePotionOfDreams;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamBubblePotionOfDreams;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool GetEnableLucidDreamHarmlessWhisper(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamHarmlessWhisper;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamHarmlessWhisper;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamHarmlessWhisper;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool HasAnyLucidDreamBenevolenceEnabled(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableLucidDreamFalseLifeline
                   || snapshot.EnableLucidDreamSmoothSailing;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.EnableLucidDreamFalseLifeline
                   || lobbySnapshot.EnableLucidDreamSmoothSailing;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableLucidDreamFalseLifeline
                   || localFallback.EnableLucidDreamSmoothSailing;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool HasAnyLucidDreamMaliceEnabled(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
        {
            return snapshot.EnableLucidDreamFishScalesMalice
                   || snapshot.EnableLucidDreamSevereWoundOneMalice
                   || snapshot.EnableLucidDreamSevereWoundTwoMalice
                   || snapshot.EnableLucidDreamMadLifeMalice
                   || snapshot.EnableLucidDreamSwampOfFateMalice
                   || snapshot.EnableLucidDreamOverpopulationMalice
                   || snapshot.EnableLucidDreamCautiousJellyfishMalice;
        }

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
        {
            return lobbySnapshot.EnableLucidDreamFishScalesMalice
                   || lobbySnapshot.EnableLucidDreamSevereWoundOneMalice
                   || lobbySnapshot.EnableLucidDreamSevereWoundTwoMalice
                   || lobbySnapshot.EnableLucidDreamMadLifeMalice
                   || lobbySnapshot.EnableLucidDreamSwampOfFateMalice
                   || lobbySnapshot.EnableLucidDreamOverpopulationMalice
                   || lobbySnapshot.EnableLucidDreamCautiousJellyfishMalice;
        }

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
        {
            return localFallback.EnableLucidDreamFishScalesMalice
                   || localFallback.EnableLucidDreamSevereWoundOneMalice
                   || localFallback.EnableLucidDreamSevereWoundTwoMalice
                   || localFallback.EnableLucidDreamMadLifeMalice
                   || localFallback.EnableLucidDreamSwampOfFateMalice
                   || localFallback.EnableLucidDreamOverpopulationMalice
                   || localFallback.EnableLucidDreamCautiousJellyfishMalice;
        }

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool HasAnyLucidDreamChaosEnabled(IRunState? runState)
    {
        if (!GetEnableLucidDream(runState))
            return false;

        if (TryGetRunSnapshot(runState, out var snapshot))
        {
            return snapshot.EnableLucidDreamFaceDeathWithComposure
                   || snapshot.EnableLucidDreamWildness
                   || snapshot.EnableLucidDreamWildnessPhantom
                   || snapshot.EnableLucidDreamPitchBlackImpulse
                   || snapshot.EnableLucidDreamBubblePotionOfDreams
                   || snapshot.EnableLucidDreamHarmlessWhisper;
        }

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
        {
            return lobbySnapshot.EnableLucidDreamFaceDeathWithComposure
                   || lobbySnapshot.EnableLucidDreamWildness
                   || lobbySnapshot.EnableLucidDreamWildnessPhantom
                   || lobbySnapshot.EnableLucidDreamPitchBlackImpulse
                   || lobbySnapshot.EnableLucidDreamBubblePotionOfDreams
                   || lobbySnapshot.EnableLucidDreamHarmlessWhisper;
        }

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
        {
            return localFallback.EnableLucidDreamFaceDeathWithComposure
                   || localFallback.EnableLucidDreamWildness
                   || localFallback.EnableLucidDreamWildnessPhantom
                   || localFallback.EnableLucidDreamPitchBlackImpulse
                   || localFallback.EnableLucidDreamBubblePotionOfDreams
                   || localFallback.EnableLucidDreamHarmlessWhisper;
        }

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return false;
    }

    public static bool HasAnyLucidDreamEnabled(IRunState? runState)
    {
        return HasAnyLucidDreamBenevolenceEnabled(runState)
               || HasAnyLucidDreamMaliceEnabled(runState)
               || HasAnyLucidDreamChaosEnabled(runState);
    }

    public static bool GetEnableDuplicatePersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return ResolveAllowDuplicates(snapshot.StartingPersonaMode);

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return ResolveAllowDuplicates(lobbySnapshot.StartingPersonaMode);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return ResolveAllowDuplicates(localFallback.StartingPersonaMode);

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return ResolveAllowDuplicates(ConfiguredStartingPersonaMode);
    }

    public static StartingPersonaDisplayMode GetStartingPersonaDisplayMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return ResolveDisplayMode(snapshot.StartingPersonaMode);

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return ResolveDisplayMode(lobbySnapshot.StartingPersonaMode);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return ResolveDisplayMode(localFallback.StartingPersonaMode);

        if (ShouldUseSafeGameplayFallback(runState))
            return StartingPersonaDisplayMode.Manual;

        return ResolveDisplayMode(ConfiguredStartingPersonaMode);
    }

    public static StartingPersonaAssignmentMode GetStartingPersonaAssignmentMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return ResolveAssignmentMode(snapshot.StartingPersonaMode);

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return ResolveAssignmentMode(lobbySnapshot.StartingPersonaMode);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return ResolveAssignmentMode(localFallback.StartingPersonaMode);

        if (ShouldUseSafeGameplayFallback(runState))
            return StartingPersonaAssignmentMode.Independent;

        return ResolveAssignmentMode(ConfiguredStartingPersonaMode);
    }

    public static StartingPersonaMode GetStartingPersonaMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.StartingPersonaMode;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.StartingPersonaMode;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.StartingPersonaMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return StartingPersonaMode.Standard;

        return ConfiguredStartingPersonaMode;
    }

    public static TokenSeriesMode GetTokenSeriesMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.TokenSeriesMode;

        if (TryGetLobbyGameplaySnapshot(out var lobbySnapshot))
            return lobbySnapshot.TokenSeriesMode;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.TokenSeriesMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return TokenSeriesMode.RandomTwo;

        return TokenSeriesMode;
    }

    public static bool GetEnablePureAngelMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnablePureAngelMode;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnablePureAngelMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnablePureAngelMode;
    }

    public static IReadOnlySet<ModelId> GetBannedPersonaRelicIds(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return DeserializeModelIdSet(snapshot.BannedRelicIdsSerialized);

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.BannedRelicIds;

        if (ShouldUseSafeGameplayFallback(runState))
            return new HashSet<ModelId>();

        return BannedPersonaRelicIds;
    }

    public static IReadOnlySet<ModelId> GetBannedRelicIds(IRunState? runState)
    {
        return GetBannedPersonaRelicIds(runState);
    }

    public static void Register()
    {
        if (_registered)
            return;

        bool migratedDefaultBannedRelics = false;
        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            store.Register<ReAstralPartyModSettings>(
                SettingsKey,
                "settings.json",
                SaveScope.Global,
                static () => new ReAstralPartyModSettings(),
                true);

            _runSettingsSnapshots = RitsuLibFramework
                .GetRunSavedDataStore(MainFile.ModId)
                .Register(
                    RunSettingsSnapshotKey,
                    static () => new ReAstralPartyRunSettingsSnapshot(),
                    new RunSavedDataOptions
                    {
                        WritePolicy = RunSavedDataWritePolicy.WhenNonDefault
                    });

            var settings = store.Get<ReAstralPartyModSettings>(SettingsKey);
            migratedDefaultBannedRelics = EnsureDefaultBannedRelicIdsInitialized(settings);
            ApplyRuntimeSettings(settings, "register");
            if (migratedDefaultBannedRelics)
                store.Save(SettingsKey);
        }

        RegisterSettingsPage();
        _registered = true;
        MainFile.Logger.Info($"{MainFile.ModId} mod settings registered.");
    }

    internal static bool TryGetStoredRunSettingsSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        snapshot = null!;
        if (runState is not RunState concreteRunState)
            return false;

        try
        {
            if (!_runSettingsSnapshots.TryGet(concreteRunState, out var storedSnapshot) || storedSnapshot == null)
                return false;

            snapshot = storedSnapshot.Clone();
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"{MainFile.ModId} failed to read stored run settings snapshot: {ex.Message}");
            return false;
        }
    }

    internal static void StoreRunSettingsSnapshot(RunState runState, ReAstralPartyRunSettingsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(snapshot);

        try
        {
            _runSettingsSnapshots.Set(runState, snapshot.Clone());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"{MainFile.ModId} failed to store run settings snapshot: {ex.Message}");
        }
    }

    internal static void SetEnableTelemetry(bool enabled)
    {
        UpdatePersistentSettings(settings => settings.EnableTelemetry = enabled, "set_enable_telemetry");
    }

    internal static void UpdateLobbyPanelState(bool isCollapsed, Vector2 globalPosition, Vector2 size)
    {
        UpdatePersistentSettings(settings =>
        {
            settings.LobbyPanelState ??= new ReAstralPartyModSettings.LobbyPanelStateModel();
            settings.LobbyPanelState.IsCollapsed = isCollapsed;
            settings.LobbyPanelState.PositionX = globalPosition.X;
            settings.LobbyPanelState.PositionY = globalPosition.Y;
            settings.LobbyPanelState.Width = size.X;
            settings.LobbyPanelState.Height = size.Y;
        }, "lobby_panel_state");
    }

    private static T ReadRuntime<T>(Func<LocalRuntimeSettings, T> selector)
    {
        lock (RuntimeSettingsGate)
        {
            return selector(_runtimeSettings);
        }
    }

    private static void RegisterSettingsPage()
    {
        var enableAllPersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableAllPersonas;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableAllPersonas = value);
                ApplyRuntimeSettings(settings, "enable_all_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.label",
                    value);
            });

        var enableStartingInitialPoint = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableStartingInitialPoint;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableStartingInitialPoint = value);
                ApplyRuntimeSettings(settings, "enable_starting_initial_point");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.label",
                    value);
            });

        var enableStartingAstralRelicStore = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableStartingAstralRelicStore;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableStartingAstralRelicStore = value);
                ApplyRuntimeSettings(settings, "enable_starting_astral_relic_store");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_astral_relic_store.label",
                    value);
            });

        var enableStartingPersonaSelection = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableStartingPersonaSelection;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableStartingPersonaSelection = value);
                ApplyRuntimeSettings(settings, "enable_starting_persona_selection");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.label",
                    value);
            });

        var enableVariantPersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableVariantPersonas;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableVariantPersonas = value);
                ApplyRuntimeSettings(settings, "enable_variant_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_variant_personas.label",
                    value);
            });

        var enableDreamSeriesEvents = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableDreamSeriesEvents;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableDreamSeriesEvents = value);
                ApplyRuntimeSettings(settings, "enable_dream_series_events");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.label",
                    value);
            });

        var enableEnigmaticSeriesEvents = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableEnigmaticSeriesEvents;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableEnigmaticSeriesEvents = value);
                ApplyRuntimeSettings(settings, "enable_enigmatic_series_events");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.label",
                    value);
            });

        var enableMoonPropShopSlots = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableMoonPropShopSlots;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableMoonPropShopSlots = value);
                ApplyRuntimeSettings(settings, "enable_moon_prop_shop_slots");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_shop_slots.label",
                    value);
            });

        var enableMoonPropRelics = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableMoonPropRelics;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableMoonPropRelics = value);
                ApplyRuntimeSettings(settings, "enable_moon_prop_relics");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_relics.label",
                    value);
            });

        var enableNeowExtraOption = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableNeowExtraOption;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableNeowExtraOption = value);
                ApplyRuntimeSettings(settings, "enable_neow_extra_option");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.label",
                    value);
            });

        var enableLucidDream = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableLucidDream;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableLucidDream = value);
                ApplyRuntimeSettings(settings, "enable_lucid_dream");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_lucid_dream.label",
                    value);
            });

        var enableCollectorsCards = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableCollectorsCards;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableCollectorsCards = value);
                ApplyRuntimeSettings(settings, "enable_collectors_cards");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_collectors_cards.label",
                    value);
            });

        var neowExtraOptionSelectionMode = ModSettingsBindings.Global<ReAstralPartyModSettings, NeowExtraOptionSelectionMode>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                var scoped = GetScopedSettingsForCurrentMode(settings);
                return NormalizeNeowExtraOptionSelectionMode(
                    scoped.EnableStartingRingOfSevenCurses,
                    scoped.NeowExtraOptionSelectionMode);
            },
            (settings, value) =>
            {
                var scoped = GetScopedSettingsForCurrentMode(settings);
                var normalizedValue = NormalizeNeowExtraOptionSelectionMode(
                    scoped.EnableStartingRingOfSevenCurses,
                    value);
                UpdateCurrentModeScopedGameplaySettings(settings, activeScoped => activeScoped.NeowExtraOptionSelectionMode = normalizedValue);
                ApplyRuntimeSettings(settings, "neow_extra_option_selection_mode");
                ShowNeowExtraOptionSelectionModeToast(normalizedValue);
            });

        var enableExtremeMode = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableExtremeMode;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableExtremeMode = value);
                ApplyRuntimeSettings(settings, "enable_extreme_mode");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label",
                    value);
            });

        var enableAllVariantPersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableAllVariantPersonas;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.EnableAllVariantPersonas = value);
                ApplyRuntimeSettings(settings, "enable_all_variant_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.label",
                    value);
            });

        var tokenSeriesMode = ModSettingsBindings.Global<ReAstralPartyModSettings, TokenSeriesMode>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).TokenSeriesMode;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.TokenSeriesMode = value);
                settings.EnableAllTokenSeries = null;
                ApplyRuntimeSettings(settings, "token_series_mode");
                ShowTokenSeriesModeToast(value);
            });

        var startingPersonaMode = ModSettingsBindings.Global<ReAstralPartyModSettings, StartingPersonaMode>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).StartingPersonaMode;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped => scoped.StartingPersonaMode = value);
                settings.EnableDuplicatePersonas = null;
                settings.EnableRandomCloneMode = null;
                settings.StartingPersonaDisplayMode = null;
                settings.StartingPersonaAssignmentMode = null;
                ApplyRuntimeSettings(settings, "starting_persona_mode");
                ShowStartingPersonaModeToast(value);
            });

        var enablePureAngelMode = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnablePureAngelMode,
            (settings, value) =>
            {
                settings.EnablePureAngelMode = value;
                ApplyRuntimeSettings(settings, "enable_pure_angel_mode");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_pure_angel_mode.label",
                    value);
            });

        var enableStartingRingOfSevenCurses = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings =>
            {
                EnsureContentModeSettingsInitialized(settings);
                return GetScopedSettingsForCurrentMode(settings).EnableStartingRingOfSevenCurses;
            },
            (settings, value) =>
            {
                UpdateCurrentModeScopedGameplaySettings(settings, scoped =>
                {
                    scoped.EnableStartingRingOfSevenCurses = value;
                    scoped.NeowExtraOptionSelectionMode = NormalizeNeowExtraOptionSelectionMode(
                        scoped.EnableStartingRingOfSevenCurses,
                        scoped.NeowExtraOptionSelectionMode);
                });
                ApplyRuntimeSettings(settings, "enable_starting_ring_of_seven_curses");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.label",
                    value);
            });

        var enablePlayRecommendation = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnablePlayRecommendation,
            (settings, value) =>
            {
                settings.EnablePlayRecommendation = value;
                ApplyRuntimeSettings(settings, "enable_play_recommendation");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_play_recommendation.label",
                    value);
            });

        var enableRouteRecommendation = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableRouteRecommendation,
            (settings, value) =>
            {
                settings.EnableRouteRecommendation = value;
                ApplyRuntimeSettings(settings, "enable_route_recommendation");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_route_recommendation.label",
                    value);
            });

        var enableTokenRecommendation = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableTokenRecommendation,
            (settings, value) =>
            {
                settings.EnableTokenRecommendation = value;
                ApplyRuntimeSettings(settings, "enable_token_recommendation");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_recommendation.label",
                    value);
            });

        var enableAutoPhrase = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableAutoPhrase,
            (settings, value) =>
            {
                settings.EnableAutoPhrase = value;
                ApplyRuntimeSettings(settings, "enable_auto_phrase");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_auto_phrase.label",
                    value);
            });

        var enableTelemetry = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableTelemetry,
            (settings, value) =>
            {
                settings.EnableTelemetry = value;
                ApplyRuntimeSettings(settings, "enable_telemetry");
                Online.AstralTelemetry.SetCollectionEnabledByConsent(value);
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.label",
                    value);
            });

        var enableStartupNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableStartupNotifications,
            (settings, value) =>
            {
                settings.EnableStartupNotifications = value;
                ApplyRuntimeSettings(settings, "enable_startup_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_startup_notifications.label",
                    value);
            });

        var enableSettingsNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableSettingsNotifications,
            (settings, value) =>
            {
                settings.EnableSettingsNotifications = value;
                ApplyRuntimeSettings(settings, "enable_settings_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_settings_notifications.label",
                    value);
            });

        var enableTelemetryNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableTelemetryNotifications,
            (settings, value) =>
            {
                settings.EnableTelemetryNotifications = value;
                ApplyRuntimeSettings(settings, "enable_telemetry_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry_notifications.label",
                    value);
            });

        var enableMultiplayerNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableMultiplayerNotifications,
            (settings, value) =>
            {
                settings.EnableMultiplayerNotifications = value;
                ApplyRuntimeSettings(settings, "enable_multiplayer_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_multiplayer_notifications.label",
                    value);
            });

        var enableConsoleCommandNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableConsoleCommandNotifications,
            (settings, value) =>
            {
                settings.EnableConsoleCommandNotifications = value;
                ApplyRuntimeSettings(settings, "enable_console_command_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_console_command_notifications.label",
                    value);
            });

        var enablePersonaRelicNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnablePersonaRelicNotifications,
            (settings, value) =>
            {
                settings.EnablePersonaRelicNotifications = value;
                ApplyRuntimeSettings(settings, "enable_persona_relic_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_persona_relic_notifications.label",
                    value);
            });

        var enableTokenRelicNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableTokenRelicNotifications,
            (settings, value) =>
            {
                settings.EnableTokenRelicNotifications = value;
                ApplyRuntimeSettings(settings, "enable_token_relic_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_relic_notifications.label",
                    value);
            });

        var enableNeowDiagnosticsNotifications = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableNeowDiagnosticsNotifications,
            (settings, value) =>
            {
                settings.EnableNeowDiagnosticsNotifications = value;
                ApplyRuntimeSettings(settings, "enable_neow_diagnostics_notifications");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_diagnostics_notifications.label",
                    value);
            });

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .WithModDisplayName(T("RE_ASTRAL_PARTY_MOD_SETTINGS.mod_display_name", "Astral Party Mod"))
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.page_title", "Mod Settings"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.page_description",
                "Gameplay switches for persona selection and token series availability."))
            .AddSection("gameplay", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.gameplay.title", "Gameplay"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.gameplay.description",
                    "These toggles apply globally across all profiles unless stated otherwise."))
                .AddToggle(
                    "enable_starting_initial_point",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.label", "Enable Starting Initial Point"),
                    enableStartingInitialPoint,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.description",
                        "At run start, every player automatically obtains Initial Point and loses 1 Gold."))
                .AddToggle(
                    "enable_starting_astral_relic_store",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_astral_relic_store.label", "Enable Astral Relic Store"),
                    enableStartingAstralRelicStore,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_astral_relic_store.description",
                        "Control whether the first actual event pull in Act 2 is replaced with Astral Relic Store."))
                .AddToggle(
                    "enable_starting_ring_of_seven_curses",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.label",
                        "Enable Starting Ring of Seven Curses"),
                    enableStartingRingOfSevenCurses,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.description",
                        "At run start, every player automatically obtains Ring of Seven Curses."))
                .AddToggle(
                    "enable_starting_persona_selection",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.label", "Enable Starting Persona Selection"),
                    enableStartingPersonaSelection,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.description",
                        "If disabled, the run skips the starting persona selection entirely and no player starts with a persona relic."))
                .AddToggle(
                    "enable_variant_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_variant_personas.label", "Enable Variant Personas"),
                    enableVariantPersonas,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_variant_personas.description",
                        "Control whether variant personas can appear during the starting persona selection for this run."))
                .AddToggle(
                    "enable_dream_series_events",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.label", "Enable Dream Series Events"),
                    enableDreamSeriesEvents,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.description",
                        "Enable Dream-prefixed Astral event content for this run."))
                .AddToggle(
                    "enable_enigmatic_series_events",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.label", "Enable Enigmatic Series Content"),
                    enableEnigmaticSeriesEvents,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.description",
                        "Enable Enigmatic-prefixed Astral content, including the corresponding Neow branch."))
                .AddToggle(
                    "enable_moon_prop_shop_slots",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_shop_slots.label", "Enable Moon Shop Slots"),
                    enableMoonPropShopSlots,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_shop_slots.description",
                        "Control whether shops always add three extra Moon relic slots below the normal inventory."))
                .AddToggle(
                    "enable_moon_prop_relics",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_relics.label", "Enable Moon Relics"),
                    enableMoonPropRelics,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_moon_prop_relics.description",
                        "Control whether normal shop inventory can naturally roll Moon relics. This does not affect the extra Moon shop slots."))
                .AddToggle(
                    "enable_neow_extra_option",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.label", "Enable Neow Extra Option"),
                    enableNeowExtraOption,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.description",
                        "Enable Astral's extra randomized fourth Neow option at run start."))
                .AddToggle(
                    "enable_lucid_dream",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_lucid_dream.label", "Enable Lucid Dream"),
                    enableLucidDream,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_lucid_dream.description",
                        "Control whether the Lucid Dream panels are shown in the room and whether Lucid Dream effects are active for this run."))
                .AddToggle(
                    "enable_collectors_cards",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_collectors_cards.label", "Enable Collectors Cards"),
                    enableCollectorsCards,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_collectors_cards.description",
                        "Control whether collectors cards can appear through normal card rewards and other standard card acquisition paths in this run."))
                .AddEnumChoice(
                    "neow_extra_option_selection_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.label",
                        "Forced Neow Extra Option"),
                    neowExtraOptionSelectionMode,
                    value => ModSettingsText.Literal(GetNeowExtraOptionSelectionModeTitle(value)),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.description",
                        "Choose the extra Neow option for this room, or keep the existing deterministic random behavior."),
                    ModSettingsChoicePresentation.Dropdown)
                .AddToggle(
                    "enable_all_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.label", "Enable All Personas"),
                    enableAllPersonas,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.description",
                        "At run start, show all registered personas instead of the default player-count-based subset."))
                .AddToggle(
                    "enable_all_variant_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.label", "Enable All Variant Personas"),
                    enableAllVariantPersonas,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.description",
                        "At run start, append all built-in variant personas to the starting persona list. Requires the Variant Personas master switch to stay enabled. Linked crossover variants are excluded. Changes apply to new runs only."))
                .AddToggle(
                    "enable_extreme_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label", "Enable Extreme Mode"),
                    enableExtremeMode,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.description",
                        "If combat is not won by the end of the player-side turn 16, that combat immediately counts as a loss."))
                .AddEnumChoice(
                    "starting_persona_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.label",
                        "Starting Persona Mode"),
                    startingPersonaMode,
                    value => value switch
                    {
                        StartingPersonaMode.Standard => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard",
                            "Standard"),
                        StartingPersonaMode.StandardDuplicate => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard_duplicate",
                            "Standard Duplicate"),
                        StartingPersonaMode.RandomAssign => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_assign",
                            "Random Assign Mode"),
                        StartingPersonaMode.Clone => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_clone",
                            "Clone Mode"),
                        StartingPersonaMode.RandomClone => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_clone",
                            "Random Clone Mode"),
                        StartingPersonaMode.DestinedClone => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_destined_clone",
                            "Destined Clone Mode"),
                        _ => ModSettingsText.Literal(value.ToString())
                    },
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.description",
                        "Choose one fixed starting-persona rule. Clicking any mode switches and saves it immediately."),
                    ModSettingsChoicePresentation.Dropdown)
                .AddEnumChoice(
                    "token_series_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.label", "Expansion Mode"),
                    tokenSeriesMode,
                    value => value switch
                    {
                        TokenSeriesMode.RandomTwo => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two",
                            "Two Random Expansions"),
                        TokenSeriesMode.All => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_all",
                            "All Expansions"),
                        TokenSeriesMode.Disabled => T(
                            "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_disabled",
                            "Disable Expansions"),
                        _ => ModSettingsText.Literal(value.ToString())
                    },
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.description",
                        "Choose whether runs use two random expansions, all expansions, or no expansion series at all."),
                    ModSettingsChoicePresentation.Dropdown)
                .WithEntryEnabledWhen("enable_variant_personas", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.VariantPersonas,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_all_variant_personas", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.AllVariantPersonas,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_dream_series_events", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.DreamSeriesEvents,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_enigmatic_series_events", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.EnigmaticSeriesEvents,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_moon_prop_shop_slots", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.MoonPropShopSlots,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_moon_prop_relics", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.MoonPropRelics,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_neow_extra_option", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.NeowExtraOption,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("neow_extra_option_selection_mode", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.NeowExtraOptionSelectionMode,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_lucid_dream", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.LucidDream,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_collectors_cards", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.CollectorsCards,
                        roomSurface: false) == AstralModeAvailability.Editable)
                .WithEntryEnabledWhen("enable_starting_ring_of_seven_curses", () =>
                    AstralContentModeRegistry.GetAvailability(
                        GetCurrentContentMode(),
                        AstralGameplaySettingKey.StartingRingOfSevenCurses,
                        roomSurface: false) == AstralModeAvailability.Editable))
            .AddSection("ban", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.ban.title", "BAN"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.ban.description",
                    "Manage content removed from related offer pools. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_relics_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.label", "BAN Personas"),
                    $"{MainFile.ModId}.banned_relics",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only.")))
            .AddSection("other", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.other.title", "Other"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.other.description",
                    "Additional reserved toggles and experimental options."))
                .AddToggle(
                    "enable_pure_angel_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_pure_angel_mode.label", "Enable Pure Angel Mode"),
                    enablePureAngelMode,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_pure_angel_mode.description",
                        "Reserved toggle. It currently has no gameplay effect."))
                .AddToggle(
                    "enable_play_recommendation",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_play_recommendation.label",
                        "Enable Play Recommendation"),
                    enablePlayRecommendation,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_play_recommendation.description",
                        "Reserved toggle. It currently has no gameplay effect."))
                .AddToggle(
                    "enable_route_recommendation",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_route_recommendation.label",
                        "Enable Route Recommendation"),
                    enableRouteRecommendation,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_route_recommendation.description",
                        "Reserved toggle. It currently has no gameplay effect."))
                .AddToggle(
                    "enable_token_recommendation",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_recommendation.label",
                        "Enable Token Recommendation"),
                    enableTokenRecommendation,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_recommendation.description",
                        "Reserved toggle. It currently has no gameplay effect."))
                .AddToggle(
                    "enable_auto_phrase",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_auto_phrase.label", "Enable Auto Phrase"),
                    enableAutoPhrase,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_auto_phrase.description",
                        "Reserved toggle. It currently has no gameplay effect."))
                .AddSubpage(
                    "compat_modules_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.label", "Compat Modules"),
                    $"{MainFile.ModId}.compat_modules",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.button", "View"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.description",
                        "Show the linked mods that Astral currently knows how to integrate with.")))
            .AddSection("telemetry", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.telemetry.title", "Telemetry"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.telemetry.description",
                    "Manage anonymous balance telemetry at any time."))
                .AddToggle(
                    "enable_telemetry",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.label", "Enable Anonymous Telemetry"),
                    enableTelemetry,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.description",
                        "Allow anonymous usage statistics for personas, tokens, and skill cards.")))
            .AddSection("notifications", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.notifications.title", "Notifications"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.notifications.description",
                    "Control general Astral toast notifications."))
                .AddToggle(
                    "enable_startup_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_startup_notifications.label", "Enable Startup Notifications"),
                    enableStartupNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_startup_notifications.description",
                        "Show startup and mod-loaded notifications."))
                .AddToggle(
                    "enable_settings_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_settings_notifications.label",
                        "Enable Settings Notifications"),
                    enableSettingsNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_settings_notifications.description",
                        "Show toast notifications when Astral settings are changed."))
                .AddToggle(
                    "enable_telemetry_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry_notifications.label",
                        "Enable Telemetry Notifications"),
                    enableTelemetryNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry_notifications.description",
                        "Show telemetry upload success, warning, and error notifications.")))
            .AddSection("multiplayer_diagnostics", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.multiplayer_diagnostics.title",
                    "Multiplayer Diagnostics"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.multiplayer_diagnostics.description",
                    "Control numbered multiplayer diagnostics for synchronization, relic obtain chains, and NEOW event-room divergence tracking."))
                .AddToggle(
                    "enable_multiplayer_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_multiplayer_notifications.label",
                        "Enable Multiplayer Notifications"),
                    enableMultiplayerNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_multiplayer_notifications.description",
                        "Show important multiplayer selection and synchronization problem notifications."))
                .AddToggle(
                    "enable_console_command_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_console_command_notifications.label",
                        "Enable Console Command Notifications"),
                    enableConsoleCommandNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_console_command_notifications.description",
                        "Show a multiplayer toast when a player executes a successful console command."))
                .AddToggle(
                    "enable_persona_relic_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_persona_relic_notifications.label",
                        "Enable Persona Relic Diagnostics"),
                    enablePersonaRelicNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_persona_relic_notifications.description",
                        "Show numbered diagnostic notifications for Astral persona, variant persona, and derivative relic problems."))
                .AddToggle(
                    "enable_token_relic_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_relic_notifications.label",
                        "Enable Token Relic Diagnostics"),
                    enableTokenRelicNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_token_relic_notifications.description",
                        "Show numbered diagnostic notifications for Astral token relic fallback, obtain, and animation problems."))
                .AddToggle(
                    "enable_neow_diagnostics_notifications",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_diagnostics_notifications.label",
                        "Enable NEOW Diagnostics"),
                    enableNeowDiagnosticsNotifications,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_diagnostics_notifications.description",
                        "Show targeted numbered diagnostics for the post-persona NEOW, Ancient layout, and event-room divergence window."))
        ));

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf(MainFile.ModId)
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_title", "BAN Personas"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_relics_navigation", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_title", "Relic Ban List"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddSubpage(
                    "banned_personas_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_persona", "Personas"),
                    $"{MainFile.ModId}.banned_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_variant_personas_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_variant_persona", "Variants"),
                    $"{MainFile.ModId}.banned_variant_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_derivatives_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_derivative", "Derivatives"),
                    $"{MainFile.ModId}.banned_derivatives",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_tokens_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_token", "Tokens"),
                    $"{MainFile.ModId}.banned_tokens",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_other_relics_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_other", "Other Relics"),
                    $"{MainFile.ModId}.banned_other_relics",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Manage content removed from relevant offer pools. Changes apply to new runs only."))),
            $"{MainFile.ModId}.banned_relics");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf($"{MainFile.ModId}.banned_relics")
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_persona", "Personas"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_personas_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_persona", "Personas"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddCustom(
                    "banned_personas_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_persona", "Personas"),
                    host => BuildBannedRelicManagerControl(host, BannedRelicCategory.Persona))),
            $"{MainFile.ModId}.banned_personas");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf($"{MainFile.ModId}.banned_relics")
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_variant_persona", "Variants"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_variant_personas_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_variant_persona", "Variants"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddCustom(
                    "banned_variant_personas_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_variant_persona", "Variants"),
                    host => BuildBannedRelicManagerControl(host, BannedRelicCategory.VariantPersona))),
            $"{MainFile.ModId}.banned_variant_personas");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf($"{MainFile.ModId}.banned_relics")
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_derivative", "Derivatives"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_derivatives_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_derivative", "Derivatives"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddCustom(
                    "banned_derivatives_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_derivative", "Derivatives"),
                    host => BuildBannedRelicManagerControl(host, BannedRelicCategory.PersonalityDerivative))),
            $"{MainFile.ModId}.banned_derivatives");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf($"{MainFile.ModId}.banned_relics")
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_token", "Tokens"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_tokens_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_token", "Tokens"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddCustom(
                    "banned_tokens_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_token", "Tokens"),
                    host => BuildBannedRelicManagerControl(host, BannedRelicCategory.Token))),
            $"{MainFile.ModId}.banned_tokens");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf($"{MainFile.ModId}.banned_relics")
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_other", "Other Relics"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Manage, by category, which content is excluded from related selection pools. Changes apply to new runs only."))
            .AddSection("banned_other_relics_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_other", "Other Relics"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move content between available and banned lists."))
                .AddCustom(
                    "banned_other_relics_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_other", "Other Relics"),
                    host => BuildBannedRelicManagerControl(host, BannedRelicCategory.Other))),
            $"{MainFile.ModId}.banned_other_relics");

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf(MainFile.ModId)
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.page_title", "Compat Modules"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.page_description",
                "Review the crossover mods Astral currently supports and whether each one is loaded."))
            .AddSection("compat_modules_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.section_title", "Linked Mods"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.section_description",
                    "This page lists the linked mods that Astral can currently integrate with."))
                .AddCustom(
                    "compat_modules_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.compat.section_title", "Linked Mods"),
                    BuildCompatModulesControl)),
            $"{MainFile.ModId}.compat_modules");
    }

    private static ModSettingsText T(string key, string fallback)
    {
        return ModSettingsText.LocString("settings_ui", key, fallback);
    }

    private static void ShowBoolSettingToast(string labelKey, bool enabled)
    {
        var title = new LocString("settings_ui", labelKey).GetRawText();
        var body = new LocString(
            "settings_ui",
            enabled
                ? "RE_ASTRAL_PARTY_MOD_SETTINGS.toast_enabled"
                : "RE_ASTRAL_PARTY_MOD_SETTINGS.toast_disabled").GetRawText();
        AstralNotificationService.ShowInfo(AstralNotificationModule.Settings, body, title);
    }

    private static void ShowTokenSeriesModeToast(TokenSeriesMode mode)
    {
        var title = new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.label").GetRawText();
        var bodyKey = mode switch
        {
            TokenSeriesMode.RandomTwo => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two",
            TokenSeriesMode.All => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_all",
            TokenSeriesMode.Disabled => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_disabled",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.toast_applied"
        };
        var body = new LocString("settings_ui", bodyKey).GetRawText();
        AstralNotificationService.ShowInfo(AstralNotificationModule.Settings, body, title);
    }

    private static void ShowStartingPersonaModeToast(StartingPersonaMode mode)
    {
        var title = new LocString("settings_ui",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.label").GetRawText();
        var bodyKey = mode switch
        {
            StartingPersonaMode.Standard =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard",
            StartingPersonaMode.StandardDuplicate =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard_duplicate",
            StartingPersonaMode.RandomAssign =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_assign",
            StartingPersonaMode.Clone =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_clone",
            StartingPersonaMode.RandomClone =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_clone",
            StartingPersonaMode.DestinedClone =>
                "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_destined_clone",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.toast_applied"
        };
        var body = new LocString("settings_ui", bodyKey).GetRawText();
        AstralNotificationService.ShowInfo(AstralNotificationModule.Settings, body, title);
    }

    private static void ShowNeowExtraOptionSelectionModeToast(NeowExtraOptionSelectionMode mode)
    {
        var title = new LocString("settings_ui",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.label").GetRawText();
        var bodyKey = GetNeowExtraOptionSelectionModeLocKey(mode) ?? "RE_ASTRAL_PARTY_MOD_SETTINGS.toast_applied";
        var body = new LocString("settings_ui", bodyKey).GetRawText();
        AstralNotificationService.ShowInfo(AstralNotificationModule.Settings, body, title);
    }

    private static void UpdatePersistentSettings(Action<ReAstralPartyModSettings> mutator, string reason)
    {
        try
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            var settings = store.Get<ReAstralPartyModSettings>(SettingsKey);
            EnsureContentModeSettingsInitialized(settings);
            mutator(settings);
            ApplyRuntimeSettings(settings, reason);
            store.Save(SettingsKey);
        }
        catch
        {
            // Ignore persistence failures and keep the current runtime snapshot unchanged.
        }
    }

    private static void ApplyRuntimeSettings(ReAstralPartyModSettings settings, string reason)
    {
        EnsureContentModeSettingsInitialized(settings);
        var snapshot = LocalRuntimeSettings.FromPersistent(settings);
        lock (RuntimeSettingsGate)
        {
            _runtimeSettings = snapshot;
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId} local runtime settings updated ({reason}): content_mode={snapshot.CurrentContentMode}, start_initial_point={snapshot.EnableStartingInitialPoint}, start_astral_relic_store={snapshot.EnableStartingAstralRelicStore}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={snapshot.EnableMoonPropShopSlots}, neow_extra_option={snapshot.EnableNeowExtraOption}, lucid_dream={snapshot.EnableLucidDream}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, variants_enabled={snapshot.EnableVariantPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonaMode}, token_series={snapshot.TokenSeriesMode}, pure_angel={snapshot.EnablePureAngelMode}, lobby_panel_collapsed={snapshot.LobbyPanelState.IsCollapsed}, lobby_panel_pos=({snapshot.LobbyPanelState.PositionX},{snapshot.LobbyPanelState.PositionY}), lobby_panel_size=({snapshot.LobbyPanelState.Width},{snapshot.LobbyPanelState.Height}), banned_relics={snapshot.BannedRelicIds.Count}, play_recommendation={snapshot.EnablePlayRecommendation}, route_recommendation={snapshot.EnableRouteRecommendation}, token_recommendation={snapshot.EnableTokenRecommendation}, auto_phrase={snapshot.EnableAutoPhrase}, telemetry={snapshot.EnableTelemetry}");
    }

    internal static StartingPersonaMode ResolveStartingPersonaMode(ReAstralPartyModSettings settings)
    {
        if (Enum.IsDefined(typeof(StartingPersonaMode), settings.StartingPersonaMode))
            return settings.StartingPersonaMode;

        if (settings.EnableRandomCloneMode == true)
            return StartingPersonaMode.RandomClone;

        if (settings.StartingPersonaDisplayMode.HasValue || settings.StartingPersonaAssignmentMode.HasValue)
        {
            var displayMode = settings.StartingPersonaDisplayMode ?? StartingPersonaDisplayMode.Manual;
            var assignmentMode = settings.StartingPersonaAssignmentMode ?? StartingPersonaAssignmentMode.Independent;
            return (displayMode, assignmentMode) switch
            {
                (StartingPersonaDisplayMode.Automatic, StartingPersonaAssignmentMode.Clone) =>
                    StartingPersonaMode.RandomClone,
                (StartingPersonaDisplayMode.Automatic, StartingPersonaAssignmentMode.Independent) =>
                    StartingPersonaMode.RandomAssign,
                (StartingPersonaDisplayMode.Manual, StartingPersonaAssignmentMode.Clone) =>
                    StartingPersonaMode.Clone,
                _ => settings.EnableDuplicatePersonas == true
                    ? StartingPersonaMode.StandardDuplicate
                    : StartingPersonaMode.Standard
            };
        }

        return settings.EnableDuplicatePersonas == true
            ? StartingPersonaMode.StandardDuplicate
            : StartingPersonaMode.Standard;
    }

    internal static StartingPersonaDisplayMode ResolveDisplayMode(StartingPersonaMode mode)
    {
        return mode switch
        {
            StartingPersonaMode.RandomAssign => StartingPersonaDisplayMode.Automatic,
            StartingPersonaMode.RandomClone => StartingPersonaDisplayMode.Automatic,
            _ => StartingPersonaDisplayMode.Manual
        };
    }

    internal static StartingPersonaAssignmentMode ResolveAssignmentMode(StartingPersonaMode mode)
    {
        return mode switch
        {
            StartingPersonaMode.Clone => StartingPersonaAssignmentMode.Clone,
            StartingPersonaMode.RandomClone => StartingPersonaAssignmentMode.Clone,
            StartingPersonaMode.DestinedClone => StartingPersonaAssignmentMode.Clone,
            _ => StartingPersonaAssignmentMode.Independent
        };
    }

    internal static bool ResolveAllowDuplicates(StartingPersonaMode mode)
    {
        return mode == StartingPersonaMode.StandardDuplicate;
    }

    internal static string GetStartingPersonaModeTitle(StartingPersonaMode mode)
    {
        var key = mode switch
        {
            StartingPersonaMode.Standard => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard",
            StartingPersonaMode.StandardDuplicate => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard_duplicate",
            StartingPersonaMode.RandomAssign => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_assign",
            StartingPersonaMode.Clone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_clone",
            StartingPersonaMode.RandomClone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_clone",
            StartingPersonaMode.DestinedClone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_destined_clone",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard"
        };
        return new LocString("settings_ui", key).GetRawText();
    }

    internal static string GetStartingPersonaModeDescription(StartingPersonaMode mode)
    {
        var key = mode switch
        {
            StartingPersonaMode.Standard => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard.description",
            StartingPersonaMode.StandardDuplicate => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard_duplicate.description",
            StartingPersonaMode.RandomAssign => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_assign.description",
            StartingPersonaMode.Clone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_clone.description",
            StartingPersonaMode.RandomClone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_random_clone.description",
            StartingPersonaMode.DestinedClone => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_destined_clone.description",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.option_standard.description"
        };
        return new LocString("settings_ui", key).GetRawText();
    }

    internal static string GetTokenSeriesModeTitle(TokenSeriesMode mode)
    {
        var key = mode switch
        {
            TokenSeriesMode.RandomTwo => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two",
            TokenSeriesMode.All => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_all",
            TokenSeriesMode.Disabled => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_disabled",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two"
        };
        return new LocString("settings_ui", key).GetRawText();
    }

    internal static string GetTokenSeriesModeDescription(TokenSeriesMode mode)
    {
        var key = mode switch
        {
            TokenSeriesMode.RandomTwo => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two.description",
            TokenSeriesMode.All => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_all.description",
            TokenSeriesMode.Disabled => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_disabled.description",
            _ => "RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.option_random_two.description"
        };
        return new LocString("settings_ui", key).GetRawText();
    }

    internal static string GetNeowExtraOptionSelectionModeTitle(NeowExtraOptionSelectionMode mode)
    {
        var key = GetNeowExtraOptionSelectionModeLocKey(mode)
                  ?? "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_default_random";
        return TryGetSettingsUiText(key, GetNeowExtraOptionSelectionModeTitleFallback(mode));
    }

    internal static string GetNeowExtraOptionSelectionModeDescription(NeowExtraOptionSelectionMode mode)
    {
        var key = GetNeowExtraOptionSelectionModeLocKey(mode, includeDescriptionSuffix: true)
                  ?? "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_default_random.description";
        return TryGetSettingsUiText(key, GetNeowExtraOptionSelectionModeDescriptionFallback(mode));
    }

    internal static NeowExtraOptionSelectionMode NormalizeNeowExtraOptionSelectionMode(
        bool enableStartingRingOfSevenCurses,
        NeowExtraOptionSelectionMode mode)
    {
        return enableStartingRingOfSevenCurses && mode == NeowExtraOptionSelectionMode.RingOfSevenCurses
            ? NeowExtraOptionSelectionMode.DefaultRandom
            : mode;
    }

    internal static string? TryGetForcedNeowExtraOptionStableKey(NeowExtraOptionSelectionMode mode)
    {
        if (mode == NeowExtraOptionSelectionMode.DefaultRandom)
            return null;

        return CamelCaseRegex.Replace(mode.ToString(), "$1_$2").ToLowerInvariant();
    }

    private static string? GetNeowExtraOptionSelectionModeLocKey(
        NeowExtraOptionSelectionMode mode,
        bool includeDescriptionSuffix = false)
    {
        if (mode == NeowExtraOptionSelectionMode.DefaultRandom)
        {
            return includeDescriptionSuffix
                ? "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_default_random.description"
                : "RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_default_random";
        }

        var stableKey = TryGetForcedNeowExtraOptionStableKey(mode);
        if (string.IsNullOrWhiteSpace(stableKey))
            return null;

        return includeDescriptionSuffix
            ? $"RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_{stableKey}.description"
            : $"RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.option_{stableKey}";
    }

    private static string TryGetSettingsUiText(string key, string fallback)
    {
        try
        {
            var locString = new LocString("settings_ui", key);
            return locString.GetRawText() ?? fallback;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] Failed to resolve settings_ui key '{key}', using fallback. {exception.GetType().Name}: {exception.Message}");
            return fallback;
        }
    }

    private static string GetNeowExtraOptionSelectionModeTitleFallback(NeowExtraOptionSelectionMode mode)
    {
        return mode switch
        {
            NeowExtraOptionSelectionMode.DefaultRandom => "Default",
            NeowExtraOptionSelectionMode.DreamFaceTheShadow => "Face the Shadow",
            NeowExtraOptionSelectionMode.RingOfSevenCurses => "Ring of Seven Curses",
            NeowExtraOptionSelectionMode.AbsoluteForm => "Absolute Form",
            NeowExtraOptionSelectionMode.ProphecySoulDevour => "Prophecy: Soul Devour",
            NeowExtraOptionSelectionMode.ProphecyReplicantGroup => "Mid Prophecy: Endless Cloning",
            NeowExtraOptionSelectionMode.DreamCoinExplosion => "Dream: Coin Explosion",
            NeowExtraOptionSelectionMode.DreamDisintegrationClaw => "Dream: Disintegration Claw",
            NeowExtraOptionSelectionMode.TetraWarforge => "Tetra: Warforge",
            _ => "Default"
        };
    }

    private static string GetNeowExtraOptionSelectionModeDescriptionFallback(NeowExtraOptionSelectionMode mode)
    {
        return mode switch
        {
            NeowExtraOptionSelectionMode.DefaultRandom =>
                "Use the current deterministic random selection from the available candidate pool.",
            NeowExtraOptionSelectionMode.DreamFaceTheShadow =>
                "Force every player's extra Neow option to be Face the Shadow.",
            NeowExtraOptionSelectionMode.RingOfSevenCurses =>
                "Force every player's extra Neow option to be Ring of Seven Curses.",
            NeowExtraOptionSelectionMode.AbsoluteForm =>
                "Force every player's extra Neow option to be Absolute Form.",
            NeowExtraOptionSelectionMode.ProphecySoulDevour =>
                "Force every player's extra Neow option to be Prophecy: Soul Devour.",
            NeowExtraOptionSelectionMode.ProphecyReplicantGroup =>
                "Force every player's extra Neow option to be Mid Prophecy: Endless Cloning.",
            NeowExtraOptionSelectionMode.DreamCoinExplosion =>
                "Force every player's extra Neow option to be Dream: Coin Explosion.",
            NeowExtraOptionSelectionMode.DreamDisintegrationClaw =>
                "Force every player's extra Neow option to be Dream: Disintegration Claw.",
            NeowExtraOptionSelectionMode.TetraWarforge =>
                "Force every player's extra Neow option to be Tetra: Warforge.",
            _ =>
                "Use the current deterministic random selection from the available candidate pool."
        };
    }

    private static Control BuildBannedRelicManagerControl(IModSettingsUiActionHost host, BannedRelicCategory category)
    {
        return new BannedRelicManagerControl(host, category);
    }

    private static Control BuildCompatModulesControl(IModSettingsUiActionHost host)
    {
        return new CompatModulesInfoControl();
    }

    private static void UpdateBannedRelicIds(IEnumerable<ModelId> bannedRelicIds)
    {
        var serialized = bannedRelicIds
            .Where(static id => id != ModelId.none)
            .Select(static id => id.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToList();

        UpdatePersistentSettings(settings =>
        {
            settings.BannedRelicIds = serialized;
            settings.BannedPersonaRelicIds = [.. serialized];
        }, "banned_relics");
    }

    private static HashSet<ModelId> DeserializeModelIdSet(IEnumerable<string>? serializedIds)
    {
        var result = new HashSet<ModelId>();
        if (serializedIds == null)
            return result;

        foreach (var entry in serializedIds)
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            try
            {
                var id = ModelId.Deserialize(entry);
                if (id != ModelId.none)
                    result.Add(id);
            }
            catch
            {
                // Ignore malformed stored ids so settings remain usable.
            }
        }

        return result;
    }

    private static IEnumerable<string>? ResolveBannedRelicIds(ReAstralPartyModSettings settings)
    {
        if (settings.BannedRelicIds is { Count: > 0 })
            return settings.BannedRelicIds;

        return settings.BannedPersonaRelicIds;
    }

    private static bool EnsureDefaultBannedRelicIdsInitialized(ReAstralPartyModSettings settings)
    {
        var beadsId = ReAstralPartyModSettings.DefaultBannedRelicIds[0];
        var serialized = new HashSet<string>(
            (ResolveBannedRelicIds(settings) ?? [])
            .Where(static id => !string.IsNullOrWhiteSpace(id)),
            StringComparer.Ordinal);

        var containsLegacyBeads = serialized.Remove(ReAstralPartyModSettings.LegacyMoonPropBeadsOfFealtyRelicId);
        if (settings.DefaultBannedRelicsInitialized && !containsLegacyBeads)
            return false;

        foreach (var relicId in ReAstralPartyModSettings.DefaultBannedRelicIds)
            serialized.Add(relicId);

        settings.BannedRelicIds = [.. serialized.OrderBy(static id => id, StringComparer.Ordinal)];
        settings.BannedPersonaRelicIds = [.. settings.BannedRelicIds];
        settings.DefaultBannedRelicsInitialized = true;
        var containsBeads = settings.BannedRelicIds.Contains(beadsId, StringComparer.Ordinal);
        MainFile.Logger.Info(
            $"{MainFile.ModId} default banned relics initialized: count={settings.BannedRelicIds.Count}, contains_beads={containsBeads}, beads_id={beadsId}");
        return true;
    }

    private static string GetRelicDisplayName(RelicModel relic)
    {
        try
        {
            var title = relic.Title.GetFormattedText();
            if (!string.IsNullOrWhiteSpace(title))
                return title;
        }
        catch
        {
            // Fall back to id when localization is missing so settings UI can still render.
        }

        return (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
    }

    private static TokenSeriesMode ResolveTokenSeriesModeCore(ReAstralPartyModSettings settings)
    {
        if (settings.EnableAllTokenSeries.HasValue)
            return settings.EnableAllTokenSeries.Value ? TokenSeriesMode.All : TokenSeriesMode.RandomTwo;

        return settings.TokenSeriesMode;
    }

    private static bool TryGetLocalAuthorityGameplayFallback(IRunState? runState, out LocalRuntimeSettings snapshot)
    {
        snapshot = default!;
        if (runState is not RunState concreteRunState)
            return false;

        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null)
            return false;

        if (LobbyGameplaySettingsSync.IsRunStartPending)
            return false;

        if (netService.Type != NetGameType.Host)
            return false;

        snapshot = ReadRuntime(static runtime => runtime);
        return true;
    }

    private static bool TryGetLobbyGameplaySnapshot(out LobbyGameplaySettingsSnapshot snapshot)
    {
        return LobbyGameplaySettingsSync.TryGetSnapshot(out snapshot);
    }

    private static bool ShouldUseSafeGameplayFallback(IRunState? runState)
    {
        if (runState is not RunState)
            return false;

        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null)
            return false;

        if (netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return false;

        if (!_loggedMissingGameplaySnapshot)
        {
            _loggedMissingGameplaySnapshot = true;
            MainFile.Logger.Warn(
                $"{MainFile.ModId} gameplay settings requested before synchronized run snapshot was available; using safe multiplayer defaults.");
        }

        return true;
    }

    private static bool IsGameplaySettingEnabledForMode(IRunState? runState, AstralGameplaySettingKey key)
    {
        return AstralContentModeRegistry.IsEnabledByMode(GetCurrentContentMode(runState), key);
    }

    private static void EnsureContentModeSettingsInitialized(ReAstralPartyModSettings settings)
    {
        EnsureDefaultBannedRelicIdsInitialized(settings);

        if (settings.ContentModeSettingsInitialized)
            return;

        settings.ModpackModeSettings = BuildModeScopedSettingsFromLegacy(settings);
        settings.VanillaModeSettings = BuildVanillaModeDefaults();
        settings.CurrentContentMode = ResolveInitialContentMode(settings);
        settings.ContentModeSettingsInitialized = true;
    }

    private static AstralContentMode ResolveInitialContentMode(ReAstralPartyModSettings settings)
    {
        if (Enum.IsDefined(typeof(AstralContentMode), settings.CurrentContentMode))
            return AstralContentModeRegistry.NormalizeMode(settings.CurrentContentMode);

        return AstralContentMode.Modpack;
    }

    private static ReAstralPartyModSettings.AstralModeScopedGameplaySettings BuildModeScopedSettingsFromLegacy(
        ReAstralPartyModSettings settings)
    {
        return new ReAstralPartyModSettings.AstralModeScopedGameplaySettings
        {
            EnableExtremeMode = settings.EnableExtremeMode,
            EnableStartingInitialPoint = settings.EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = settings.EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = settings.EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = settings.EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = settings.EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = settings.EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = settings.EnableMoonPropShopSlots,
            EnableMoonPropRelics = settings.EnableMoonPropRelics,
            EnableNeowExtraOption = settings.EnableNeowExtraOption,
            EnableLucidDream = settings.EnableLucidDream,
            EnableCollectorsCards = settings.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NormalizeNeowExtraOptionSelectionMode(
                settings.EnableStartingRingOfSevenCurses,
                settings.NeowExtraOptionSelectionMode),
            EnableAllPersonas = settings.EnableAllPersonas,
            EnableVariantPersonas = settings.EnableVariantPersonas,
            EnableAllVariantPersonas = settings.EnableAllVariantPersonas,
            StartingPersonaMode = ResolveStartingPersonaMode(settings),
            TokenSeriesMode = ResolveTokenSeriesModeCore(settings)
        };
    }

    private static ReAstralPartyModSettings.AstralModeScopedGameplaySettings BuildVanillaModeDefaults()
    {
        return new ReAstralPartyModSettings.AstralModeScopedGameplaySettings
        {
            EnableExtremeMode = false,
            EnableStartingInitialPoint = false,
            EnableStartingAstralRelicStore = true,
            EnableStartingRingOfSevenCurses = false,
            EnableStartingPersonaSelection = true,
            EnableDreamSeriesEvents = false,
            EnableEnigmaticSeriesEvents = false,
            EnableMoonPropShopSlots = false,
            EnableMoonPropRelics = false,
            EnableNeowExtraOption = false,
            EnableLucidDream = false,
            EnableCollectorsCards = false,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode.DefaultRandom,
            EnableAllPersonas = false,
            EnableVariantPersonas = false,
            EnableAllVariantPersonas = false,
            StartingPersonaMode = StartingPersonaMode.Standard,
            TokenSeriesMode = TokenSeriesMode.RandomTwo
        };
    }

    private static ReAstralPartyModSettings.AstralModeScopedGameplaySettings GetScopedSettingsForCurrentMode(
        ReAstralPartyModSettings settings)
    {
        EnsureContentModeSettingsInitialized(settings);
        var mode = AstralContentModeRegistry.NormalizeMode(settings.CurrentContentMode);
        return mode == AstralContentMode.Modpack
            ? settings.ModpackModeSettings ??= BuildModeScopedSettingsFromLegacy(settings)
            : settings.VanillaModeSettings ??= BuildVanillaModeDefaults();
    }

    private static void UpdateCurrentModeScopedGameplaySettings(
        ReAstralPartyModSettings settings,
        Action<ReAstralPartyModSettings.AstralModeScopedGameplaySettings> mutator)
    {
        EnsureContentModeSettingsInitialized(settings);
        mutator(GetScopedSettingsForCurrentMode(settings));
        SyncLegacyGameplayFields(settings);
    }

    private static void SyncLegacyGameplayFields(ReAstralPartyModSettings settings)
    {
        EnsureContentModeSettingsInitialized(settings);
        var modpack = settings.ModpackModeSettings ??= BuildModeScopedSettingsFromLegacy(settings);
        settings.EnableExtremeMode = modpack.EnableExtremeMode;
        settings.EnableStartingInitialPoint = modpack.EnableStartingInitialPoint;
        settings.EnableStartingAstralRelicStore = modpack.EnableStartingAstralRelicStore;
        settings.EnableStartingRingOfSevenCurses = modpack.EnableStartingRingOfSevenCurses;
        settings.EnableStartingPersonaSelection = modpack.EnableStartingPersonaSelection;
        settings.EnableDreamSeriesEvents = modpack.EnableDreamSeriesEvents;
        settings.EnableEnigmaticSeriesEvents = modpack.EnableEnigmaticSeriesEvents;
        settings.EnableMoonPropShopSlots = modpack.EnableMoonPropShopSlots;
        settings.EnableMoonPropRelics = modpack.EnableMoonPropRelics;
        settings.EnableNeowExtraOption = modpack.EnableNeowExtraOption;
        settings.EnableLucidDream = modpack.EnableLucidDream;
        settings.EnableCollectorsCards = modpack.EnableCollectorsCards;
        settings.NeowExtraOptionSelectionMode = modpack.NeowExtraOptionSelectionMode;
        settings.EnableAllPersonas = modpack.EnableAllPersonas;
        settings.EnableVariantPersonas = modpack.EnableVariantPersonas;
        settings.EnableAllVariantPersonas = modpack.EnableAllVariantPersonas;
        settings.StartingPersonaMode = modpack.StartingPersonaMode;
        settings.TokenSeriesMode = modpack.TokenSeriesMode;
    }

    private sealed class LocalRuntimeSettings
    {
        public sealed class LobbyPanelStateSnapshot
        {
            public bool IsCollapsed { get; init; }

            public float PositionX { get; init; } = 1140f;

            public float PositionY { get; init; } = 120f;

            public float Width { get; init; } = 440f;

            public float Height { get; init; } = 520f;
        }

        public IReadOnlySet<ModelId> BannedRelicIds { get; init; } = new HashSet<ModelId>();

        public IReadOnlySet<ModelId> BannedPersonaRelicIds => BannedRelicIds;

        public AstralContentMode CurrentContentMode { get; init; } = AstralContentMode.Vanilla;

        public bool EnableExtremeMode { get; init; }

        public bool EnableStartingInitialPoint { get; init; }

        public bool EnableStartingAstralRelicStore { get; init; } = true;

        public bool EnableStartingRingOfSevenCurses { get; init; }

        public bool EnableStartingPersonaSelection { get; init; } = true;

        public bool EnableDreamSeriesEvents { get; init; } = true;

        public bool EnableEnigmaticSeriesEvents { get; init; } = true;

        public bool EnableMoonPropShopSlots { get; init; } = true;

        public bool EnableMoonPropRelics { get; init; } = true;

        public bool EnableNeowExtraOption { get; init; } = true;

        public bool EnableLucidDream { get; init; } = true;

        public bool EnableCollectorsCards { get; init; } = true;

        public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; init; } =
            NeowExtraOptionSelectionMode.DefaultRandom;

        public bool EnableAllPersonas { get; init; }

        public bool EnableVariantPersonas { get; init; } = true;

        public bool EnableAllVariantPersonas { get; init; }

        public StartingPersonaMode StartingPersonaMode { get; init; } = StartingPersonaMode.Standard;

        public bool EnablePlayRecommendation { get; init; }

        public bool EnableRouteRecommendation { get; init; }

        public bool EnableTokenRecommendation { get; init; }

        public bool EnableAutoPhrase { get; init; }

        public bool EnableTelemetry { get; init; } = true;

        public bool EnableStartupNotifications { get; init; } = true;

        public bool EnableSettingsNotifications { get; init; } = true;

        public bool EnableTelemetryNotifications { get; init; } = true;

        public bool EnableMultiplayerNotifications { get; init; } = true;

        public bool EnableConsoleCommandNotifications { get; init; } = true;

        public bool EnablePersonaRelicNotifications { get; init; } = true;

        public bool EnableTokenRelicNotifications { get; init; } = true;

        public bool EnableNeowDiagnosticsNotifications { get; init; } = true;

        public TokenSeriesMode TokenSeriesMode { get; init; } = TokenSeriesMode.RandomTwo;

        public bool EnablePureAngelMode { get; init; } = true;

        public bool EnableLucidDreamFalseLifeline { get; init; }

        public bool EnableLucidDreamSmoothSailing { get; init; }

        public bool EnableLucidDreamFishScalesMalice { get; init; }

        public bool EnableLucidDreamSevereWoundOneMalice { get; init; }

        public bool EnableLucidDreamSevereWoundTwoMalice { get; init; }

        public bool EnableLucidDreamMadLifeMalice { get; init; }

        public bool EnableLucidDreamSwampOfFateMalice { get; init; }

        public bool EnableLucidDreamOverpopulationMalice { get; init; }

        public bool EnableLucidDreamCautiousJellyfishMalice { get; init; }

        public bool EnableLucidDreamFaceDeathWithComposure { get; init; }

        public bool EnableLucidDreamWildness { get; init; }

        public bool EnableLucidDreamWildnessPhantom { get; init; }

        public bool EnableLucidDreamPitchBlackImpulse { get; init; }

        public bool EnableLucidDreamBubblePotionOfDreams { get; init; }

        public bool EnableLucidDreamHarmlessWhisper { get; init; }

        public LobbyPanelStateSnapshot LobbyPanelState { get; init; } = new();

        public static LocalRuntimeSettings FromPersistent(ReAstralPartyModSettings settings)
        {
            EnsureContentModeSettingsInitialized(settings);
            var scoped = GetScopedSettingsForCurrentMode(settings);
            return new LocalRuntimeSettings
            {
                BannedRelicIds = DeserializeModelIdSet(ResolveBannedRelicIds(settings)),
                CurrentContentMode = AstralContentModeRegistry.NormalizeMode(settings.CurrentContentMode),
                EnableExtremeMode = scoped.EnableExtremeMode,
                EnableStartingInitialPoint = scoped.EnableStartingInitialPoint,
                EnableStartingAstralRelicStore = scoped.EnableStartingAstralRelicStore,
                EnableStartingRingOfSevenCurses = scoped.EnableStartingRingOfSevenCurses,
                EnableStartingPersonaSelection = scoped.EnableStartingPersonaSelection,
                EnableDreamSeriesEvents = scoped.EnableDreamSeriesEvents,
                EnableEnigmaticSeriesEvents = scoped.EnableEnigmaticSeriesEvents,
                EnableMoonPropShopSlots = scoped.EnableMoonPropShopSlots,
                EnableMoonPropRelics = scoped.EnableMoonPropRelics,
                EnableNeowExtraOption = scoped.EnableNeowExtraOption,
                EnableLucidDream = scoped.EnableLucidDream,
                EnableCollectorsCards = scoped.EnableCollectorsCards,
                NeowExtraOptionSelectionMode = scoped.NeowExtraOptionSelectionMode,
                EnableAllPersonas = scoped.EnableAllPersonas,
                EnableVariantPersonas = scoped.EnableVariantPersonas,
                EnableAllVariantPersonas = scoped.EnableAllVariantPersonas,
                StartingPersonaMode = scoped.StartingPersonaMode,
                EnablePlayRecommendation = settings.EnablePlayRecommendation,
                EnableRouteRecommendation = settings.EnableRouteRecommendation,
                EnableTokenRecommendation = settings.EnableTokenRecommendation,
                EnableAutoPhrase = settings.EnableAutoPhrase,
                EnableTelemetry = settings.EnableTelemetry,
                EnableStartupNotifications = settings.EnableStartupNotifications,
                EnableSettingsNotifications = settings.EnableSettingsNotifications,
                EnableTelemetryNotifications = settings.EnableTelemetryNotifications,
                EnableMultiplayerNotifications = settings.EnableMultiplayerNotifications,
                EnableConsoleCommandNotifications = settings.EnableConsoleCommandNotifications,
                EnablePersonaRelicNotifications = settings.EnablePersonaRelicNotifications,
                EnableTokenRelicNotifications = settings.EnableTokenRelicNotifications,
                EnableNeowDiagnosticsNotifications = settings.EnableNeowDiagnosticsNotifications,
                TokenSeriesMode = scoped.TokenSeriesMode,
                EnablePureAngelMode = settings.EnablePureAngelMode,
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
                LobbyPanelState = new LobbyPanelStateSnapshot
                {
                    IsCollapsed = settings.LobbyPanelState?.IsCollapsed ?? false,
                    PositionX = settings.LobbyPanelState?.PositionX ?? 1140f,
                    PositionY = settings.LobbyPanelState?.PositionY ?? 120f,
                    Width = settings.LobbyPanelState?.Width ?? 440f,
                    Height = settings.LobbyPanelState?.Height ?? 520f
                }
            };
        }
    }

    private sealed partial class CompatModulesInfoControl : VBoxContainer
    {
        public CompatModulesInfoControl()
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            AddThemeConstantOverride("separation", 12);

            var intro = new Label
            {
                Text = new LocString(
                    "settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.intro").GetRawText(),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            AddChild(intro);

            var tableCard = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            tableCard.AddThemeStyleboxOverride("panel", CreateCompatTableCardStyle());
            AddChild(tableCard);

            var tableRoot = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            tableRoot.AddThemeConstantOverride("separation", 0);
            tableCard.AddChild(tableRoot);

            var headerRow = BuildCompatRow(
                new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.column_name").GetRawText(),
                new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.column_id").GetRawText(),
                new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.column_status").GetRawText(),
                null,
                false,
                true,
                null);
            tableRoot.AddChild(headerRow);

            var supportedMods = OptionalModCompatRegistry.GetSupportedMods();
            for (var index = 0; index < supportedMods.Count; index++)
            {
                var compatMod = supportedMods[index];
                var isLoaded = OptionalModCompatRegistry.IsModLoaded(compatMod.ModId);
                var statusText = new LocString(
                    "settings_ui",
                    isLoaded
                        ? "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.status_loaded"
                        : "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.status_not_loaded").GetRawText();
                tableRoot.AddChild(BuildCompatRow(
                    compatMod.DisplayName,
                    compatMod.ModId,
                    statusText,
                    compatMod.ReleasePageUrl,
                    isLoaded,
                    false,
                    index % 2 == 0));
            }
        }

        private static PanelContainer BuildCompatRow(
            string name,
            string modId,
            string status,
            string? releasePageUrl,
            bool loaded,
            bool header,
            bool? alternate)
        {
            var row = new PanelContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            row.AddThemeStyleboxOverride("panel", CreateCompatRowStyle(header, alternate));

            var rowLayout = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            rowLayout.AddThemeConstantOverride("separation", 20);
            row.AddChild(rowLayout);

            rowLayout.AddChild(CreateCompatColumn(name, header, 0.38f));
            rowLayout.AddChild(CreateCompatColumn(modId, header, 0.42f));
            rowLayout.AddChild(CreateCompatStatusColumn(status, releasePageUrl, loaded, header, 0.20f));
            return row;
        }

        private static Control CreateCompatColumn(string text, bool header, float ratio)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = ratio,
                MouseFilter = MouseFilterEnum.Ignore
            };
            column.AddChild(CreateCompatTextCell(text, header));
            return column;
        }

        private static Control CreateCompatStatusColumn(
            string text,
            string? releasePageUrl,
            bool loaded,
            bool header,
            float ratio)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsStretchRatio = ratio,
                MouseFilter = MouseFilterEnum.Ignore
            };

            if (header || string.IsNullOrWhiteSpace(releasePageUrl))
            {
                column.AddChild(CreateCompatTextCell(text, header, loaded));
                return column;
            }

            var linkButton = new LinkButton
            {
                Text = text,
                Uri = releasePageUrl,
                Underline = LinkButton.UnderlineMode.Always,
                TooltipText = releasePageUrl,
                SizeFlagsHorizontal = SizeFlags.ShrinkBegin
            };
            linkButton.AddThemeColorOverride("font_color", loaded
                ? new Color(0.42f, 0.9f, 0.56f)
                : new Color(0.95f, 0.38f, 0.38f));
            linkButton.AddThemeColorOverride("font_hover_color", loaded
                ? new Color(0.58f, 0.98f, 0.68f)
                : new Color(1f, 0.52f, 0.52f));
            linkButton.Pressed += () => OpenCompatLink(releasePageUrl);
            column.AddChild(linkButton);
            return column;
        }

        private static void OpenCompatLink(string releasePageUrl)
        {
            var error = OS.ShellOpen(releasePageUrl);
            if (error != Error.Ok)
            {
                MainFile.Logger.Warn(
                    $"[{MainFile.ModId}] Failed to open compat link '{releasePageUrl}': {error}");
            }
        }

        private static Label CreateCompatTextCell(string text, bool header, bool loaded = false)
        {
            var label = new Label
            {
                Text = text,
                AutowrapMode = TextServer.AutowrapMode.Off,
                ClipText = true,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            if (header)
            {
                label.AddThemeColorOverride("font_color", new Color(0.96f, 0.84f, 0.48f));
            }
            else if (text == new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.status_loaded").GetRawText()
                     || text == new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.compat.status_not_loaded").GetRawText())
            {
                label.AddThemeColorOverride("font_color", loaded
                    ? new Color(0.42f, 0.9f, 0.56f)
                    : new Color(0.95f, 0.38f, 0.38f));
            }
            return label;
        }

        private static StyleBoxFlat CreateCompatTableCardStyle()
        {
            return new StyleBoxFlat
            {
                BgColor = new Color(0.08f, 0.1f, 0.14f, 0.92f),
                BorderColor = new Color(0.72f, 0.64f, 0.38f, 0.75f),
                BorderWidthBottom = 2,
                BorderWidthTop = 2,
                BorderWidthLeft = 2,
                BorderWidthRight = 2,
                CornerRadiusTopLeft = 12,
                CornerRadiusTopRight = 12,
                CornerRadiusBottomLeft = 12,
                CornerRadiusBottomRight = 12,
                ContentMarginLeft = 12,
                ContentMarginTop = 12,
                ContentMarginRight = 12,
                ContentMarginBottom = 12
            };
        }

        private static StyleBoxFlat CreateCompatRowStyle(bool header, bool? alternate)
        {
            var background = header
                ? new Color(0.2f, 0.16f, 0.08f, 0.95f)
                : alternate == true
                    ? new Color(0.14f, 0.17f, 0.22f, 0.78f)
                    : new Color(0.11f, 0.13f, 0.18f, 0.7f);
            var border = header
                ? new Color(0.9f, 0.76f, 0.35f, 0.8f)
                : new Color(0.38f, 0.44f, 0.58f, 0.45f);

            return new StyleBoxFlat
            {
                BgColor = background,
                BorderColor = border,
                BorderWidthBottom = 1,
                ContentMarginLeft = 12,
                ContentMarginTop = 10,
                ContentMarginRight = 12,
                ContentMarginBottom = 10
            };
        }
    }

    private sealed partial class BannedRelicManagerControl : VBoxContainer
    {
        private readonly IModSettingsUiActionHost _host;
        private readonly BannedRelicCategory _category;
        private readonly ItemList _availableList;
        private readonly ItemList _bannedList;
        private readonly Label _summaryLabel;
        private List<RelicModel> _availableRelics = new();
        private List<RelicModel> _bannedRelics = new();

        public BannedRelicManagerControl(IModSettingsUiActionHost host, BannedRelicCategory category)
        {
            _host = host;
            _category = category;
            SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SizeFlagsVertical = SizeFlags.ExpandFill;
            MouseFilter = MouseFilterEnum.Ignore;
            AddThemeConstantOverride("separation", 12);

            var intro = new Label
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description").GetRawText(),
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(intro);

            _summaryLabel = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(_summaryLabel);

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            row.AddThemeConstantOverride("separation", 12);
            AddChild(row);

            row.AddChild(BuildColumn(
                new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.available_title").GetRawText(),
                out _availableList));
            row.AddChild(BuildActionColumn());
            row.AddChild(BuildColumn(
                new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.banned_title").GetRawText(),
                out _bannedList));

            _availableList.ItemActivated += index => BanSelectedRelic(index);
            _bannedList.ItemActivated += index => UnbanSelectedRelic(index);

            var footer = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                Alignment = BoxContainer.AlignmentMode.End
            };
            footer.AddThemeConstantOverride("separation", 10);
            AddChild(footer);

            footer.AddChild(new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.ban_all").GetRawText()
            });
            ((Button)footer.GetChild(0)).Pressed += () =>
            {
                var updated = new HashSet<ModelId>(BannedRelicIds);
                foreach (var relic in BannedRelicRegistry.GetCanonicalRelics(_category))
                    updated.Add(relic.CanonicalInstance?.Id ?? relic.Id);

                UpdateBannedRelicIds(updated);
                RefreshLists();
                NotifyChanged();
            };

            footer.AddChild(new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.clear_all").GetRawText()
            });
            ((Button)footer.GetChild(1)).Pressed += () =>
            {
                var updated = new HashSet<ModelId>(BannedRelicIds);
                foreach (var relic in BannedRelicRegistry.GetCanonicalRelics(_category))
                    updated.Remove(relic.CanonicalInstance?.Id ?? relic.Id);

                UpdateBannedRelicIds(updated);
                RefreshLists();
                NotifyChanged();
            };

            RefreshLists();
        }

        private VBoxContainer BuildColumn(string titleText, out ItemList itemList)
        {
            var column = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            column.AddThemeConstantOverride("separation", 8);

            column.AddChild(new Label
            {
                Text = titleText,
                MouseFilter = MouseFilterEnum.Ignore
            });

            itemList = new ItemList
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(320f, 320f),
                SelectMode = ItemList.SelectModeEnum.Single
            };
            column.AddChild(itemList);
            return column;
        }

        private VBoxContainer BuildActionColumn()
        {
            var column = new VBoxContainer
            {
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Ignore
            };
            column.AddThemeConstantOverride("separation", 10);

            var addButton = new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.add_button").GetRawText(),
                CustomMinimumSize = new Vector2(120f, 0f)
            };
            addButton.Pressed += BanSelectedRelic;
            column.AddChild(addButton);

            var removeButton = new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.remove_button").GetRawText(),
                CustomMinimumSize = new Vector2(120f, 0f)
            };
            removeButton.Pressed += UnbanSelectedRelic;
            column.AddChild(removeButton);

            return column;
        }

        private void RefreshLists()
        {
            var bannedIds = BannedRelicIds;
            var allRelics = BannedRelicRegistry.GetCanonicalRelics(_category)
                .OrderBy(GetRelicDisplayName, StringComparer.Ordinal)
                .ToList();
            _availableRelics = allRelics
                .Where(relic => !bannedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
                .ToList();
            _bannedRelics = allRelics
                .Where(relic => bannedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
                .ToList();

            RebuildItemList(_availableList, _availableRelics);
            RebuildItemList(_bannedList, _bannedRelics);
            _summaryLabel.Text = string.Format(
                new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.summary").GetRawText(),
                _bannedRelics.Count,
                allRelics.Count);
        }

        private static void RebuildItemList(ItemList list, IReadOnlyList<RelicModel> relics)
        {
            list.Clear();
            foreach (var relic in relics)
                list.AddItem(GetRelicDisplayName(relic));
        }

        private void BanSelectedRelic()
        {
            var index = _availableList.GetSelectedItems().FirstOrDefault(-1);
            BanSelectedRelic(index);
        }

        private void BanSelectedRelic(long index)
        {
            var selectedIndex = (int)index;
            if (selectedIndex < 0 || selectedIndex >= _availableRelics.Count)
                return;

            var updated = new HashSet<ModelId>(BannedRelicIds)
            {
                _availableRelics[selectedIndex].CanonicalInstance?.Id ?? _availableRelics[selectedIndex].Id
            };
            UpdateBannedRelicIds(updated);
            RefreshLists();
            NotifyChanged();
        }

        private void UnbanSelectedRelic()
        {
            var index = _bannedList.GetSelectedItems().FirstOrDefault(-1);
            UnbanSelectedRelic(index);
        }

        private void UnbanSelectedRelic(long index)
        {
            var selectedIndex = (int)index;
            if (selectedIndex < 0 || selectedIndex >= _bannedRelics.Count)
                return;

            var updated = new HashSet<ModelId>(BannedRelicIds);
            updated.Remove(_bannedRelics[selectedIndex].CanonicalInstance?.Id ?? _bannedRelics[selectedIndex].Id);
            UpdateBannedRelicIds(updated);
            RefreshLists();
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            if (EnableSettingsNotifications)
            {
                AstralNotificationService.ShowInfo(
                    AstralNotificationModule.Settings,
                    new LocString("settings_ui",
                        "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.toast_body").GetRawText(),
                    new LocString("settings_ui",
                        "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.label").GetRawText());
            }

            _host.RequestRefresh();
        }
    }

}
