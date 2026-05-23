using Godot;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public enum TokenSeriesMode
{
    RandomTwo = 0,
    All = 1,
    Disabled = 2
}

public sealed class ReAstralPartyModSettings
{
    public List<string> BannedRelicIds { get; set; } = new();

    // Legacy field kept so older settings.json files still load cleanly.
    public List<string>? BannedPersonaRelicIds { get; set; }

    public bool EnableExtremeMode { get; set; }

    public bool EnableAllPersonas { get; set; }

    public bool EnableAllVariantPersonas { get; set; }

    public bool EnableDuplicatePersonas { get; set; }

    public bool EnableRandomCloneMode { get; set; }

    public bool EnablePlayRecommendation { get; set; }

    public bool EnableRouteRecommendation { get; set; }

    public bool EnableTokenRecommendation { get; set; }

    public bool EnableAutoPhrase { get; set; }

    public bool EnableTelemetry { get; set; } = true;

    public bool EnableStartupNotifications { get; set; } = true;

    public bool EnableSettingsNotifications { get; set; } = true;

    public bool EnableTelemetryNotifications { get; set; } = true;

    public bool EnableMultiplayerNotifications { get; set; } = true;

    public bool EnablePersonaRelicNotifications { get; set; } = true;

    public bool EnableTokenRelicNotifications { get; set; } = true;

    public bool EnableNeowDiagnosticsNotifications { get; set; } = true;

    // Legacy bool setting kept for backward compatibility with older settings.json files.
    public bool? EnableAllTokenSeries { get; set; }

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;
}

public static partial class ReAstralPartyModSettingsManager
{
    public const string SettingsKey = "settings";

    private static readonly object RuntimeSettingsGate = new();

    private static LocalRuntimeSettings _runtimeSettings = LocalRuntimeSettings.FromPersistent(
        new ReAstralPartyModSettings());

    private static bool _registered;
    private static bool _loggedMissingGameplaySnapshot;

    public static bool EnableAllPersonas => ReadRuntime(settings => settings.EnableAllPersonas);

    public static bool EnableAllVariantPersonas => ReadRuntime(settings => settings.EnableAllVariantPersonas);

    public static bool EnableExtremeMode => ReadRuntime(settings => settings.EnableExtremeMode);

    public static bool EnableDuplicatePersonas => ReadRuntime(settings => settings.EnableDuplicatePersonas);

    public static bool EnableRandomCloneMode => ReadRuntime(settings => settings.EnableRandomCloneMode);

    public static TokenSeriesMode TokenSeriesMode => ReadRuntime(settings => settings.TokenSeriesMode);

    public static bool EnablePureAngelMode => ReadRuntime(settings => settings.EnablePureAngelMode);

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
            return RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey);
        }
        catch
        {
            return new ReAstralPartyModSettings();
        }
    }

    public static TokenSeriesMode ResolveTokenSeriesMode(ReAstralPartyModSettings settings)
    {
        return ResolveTokenSeriesModeCore(settings);
    }

    public static bool TryGetRunSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        return ReAstralPartyRunSettingsSync.TryGetSnapshot(runState, out snapshot);
    }

    public static bool GetEnableAllPersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableAllPersonas;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableAllPersonas;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableAllPersonas;
    }

    public static bool GetEnableAllVariantPersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableAllVariantPersonas;

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

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableExtremeMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableExtremeMode;
    }

    public static bool GetEnableDuplicatePersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableDuplicatePersonas;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableDuplicatePersonas;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableDuplicatePersonas;
    }

    public static bool GetEnableRandomCloneMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableRandomCloneMode;

        if (TryGetLocalAuthorityGameplayFallback(runState, out var localFallback))
            return localFallback.EnableRandomCloneMode;

        if (ShouldUseSafeGameplayFallback(runState))
            return false;

        return EnableRandomCloneMode;
    }

    public static TokenSeriesMode GetTokenSeriesMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.TokenSeriesMode;

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

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            store.Register<ReAstralPartyModSettings>(
                SettingsKey,
                "settings.json",
                SaveScope.Global,
                static () => new ReAstralPartyModSettings(),
                true);
        }

        ApplyRuntimeSettings(ReadLocalSettings(), "register");
        RegisterSettingsPage();
        _registered = true;
        MainFile.Logger.Info($"{MainFile.ModId} mod settings registered.");
    }

    internal static void SetEnableTelemetry(bool enabled)
    {
        UpdatePersistentSettings(settings => settings.EnableTelemetry = enabled, "set_enable_telemetry");
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
            settings => settings.EnableAllPersonas,
            (settings, value) =>
            {
                settings.EnableAllPersonas = value;
                ApplyRuntimeSettings(settings, "enable_all_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.label",
                    value);
            });

        var enableExtremeMode = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableExtremeMode,
            (settings, value) =>
            {
                settings.EnableExtremeMode = value;
                ApplyRuntimeSettings(settings, "enable_extreme_mode");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label",
                    value);
            });

        var enableAllVariantPersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableAllVariantPersonas,
            (settings, value) =>
            {
                settings.EnableAllVariantPersonas = value;
                ApplyRuntimeSettings(settings, "enable_all_variant_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.label",
                    value);
            });

        var enableDuplicatePersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableDuplicatePersonas,
            (settings, value) =>
            {
                settings.EnableDuplicatePersonas = value;
                ApplyRuntimeSettings(settings, "enable_duplicate_personas");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_duplicate_personas.label",
                    value);
            });

        var enableRandomCloneMode = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableRandomCloneMode,
            (settings, value) =>
            {
                settings.EnableRandomCloneMode = value;
                ApplyRuntimeSettings(settings, "enable_random_clone_mode");
                ShowBoolSettingToast(
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.enable_random_clone_mode.label",
                    value);
            });

        var tokenSeriesMode = ModSettingsBindings.Global<ReAstralPartyModSettings, TokenSeriesMode>(
            MainFile.ModId,
            SettingsKey,
            settings => ResolveTokenSeriesModeCore(settings),
            (settings, value) =>
            {
                settings.TokenSeriesMode = value;
                settings.EnableAllTokenSeries = null;
                ApplyRuntimeSettings(settings, "token_series_mode");
                ShowTokenSeriesModeToast(value);
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
                        "At run start, append all built-in variant personas to the starting persona list. Linked crossover variants are excluded. Changes apply to new runs only."))
                .AddToggle(
                    "enable_extreme_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label", "Enable Extreme Mode"),
                    enableExtremeMode,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.description",
                        "If combat is not won by the end of the player-side turn 16, that combat immediately counts as a loss."))
                .AddToggle(
                    "enable_duplicate_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_duplicate_personas.label", "Enable Duplicate Personas"),
                    enableDuplicatePersonas,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_duplicate_personas.description",
                        "Multiple players can choose the same starting persona without conflict."))
                .AddToggle(
                    "enable_random_clone_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_random_clone_mode.label", "Enable Random Clone Mode"),
                    enableRandomCloneMode,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_random_clone_mode.description",
                        "Skip starting persona selection and give every player the same random persona. Changes apply to new runs only."))
                .AddSubpage(
                    "banned_personas_subpage",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.label", "BAN Personas"),
                    $"{MainFile.ModId}.banned_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.button", "Manage"),
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.description",
                        "Banned personas are removed from persona selection lists. Changes apply to new runs only."))
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
                    ModSettingsChoicePresentation.Dropdown))
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
                        "Show targeted numbered diagnostics for the post-persona NEOW, Ancient layout, and event-room divergence window."))));

        RitsuLibFramework.RegisterModSettings(MainFile.ModId, page => page
            .AsChildOf(MainFile.ModId)
            .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_title", "BAN Personas"))
            .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.page_description",
                "Choose which personas are excluded from persona selection lists. Changes apply to new runs only."))
            .AddSection("banned_personas_manager", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_title", "Persona Ban List"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_description",
                    "Move personas between available and banned lists."))
                .AddCustom(
                    "banned_personas_manager_control",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.section_title", "Persona Ban List"),
                    BuildBannedPersonaManagerControl)),
            $"{MainFile.ModId}.banned_personas");

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

    private static void UpdatePersistentSettings(Action<ReAstralPartyModSettings> mutator, string reason)
    {
        try
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            var settings = store.Get<ReAstralPartyModSettings>(SettingsKey);
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
        var snapshot = LocalRuntimeSettings.FromPersistent(settings);
        lock (RuntimeSettingsGate)
        {
            _runtimeSettings = snapshot;
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId} local runtime settings updated ({reason}): all_personas={snapshot.EnableAllPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, duplicate_personas={snapshot.EnableDuplicatePersonas}, random_clone_mode={snapshot.EnableRandomCloneMode}, token_series={snapshot.TokenSeriesMode}, pure_angel={snapshot.EnablePureAngelMode}, banned_relics={snapshot.BannedRelicIds.Count}, play_recommendation={snapshot.EnablePlayRecommendation}, route_recommendation={snapshot.EnableRouteRecommendation}, token_recommendation={snapshot.EnableTokenRecommendation}, auto_phrase={snapshot.EnableAutoPhrase}, telemetry={snapshot.EnableTelemetry}");
    }

    private static Control BuildBannedPersonaManagerControl(IModSettingsUiActionHost host)
    {
        return new BannedRelicManagerControl(host);
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

    private static string GetRelicDisplayName(RelicModel relic)
    {
        return relic.Title.GetFormattedText();
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

        if (netService.Type != NetGameType.Host)
            return false;

        snapshot = ReadRuntime(static runtime => runtime);
        return true;
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

    private sealed class LocalRuntimeSettings
    {
        public IReadOnlySet<ModelId> BannedRelicIds { get; init; } = new HashSet<ModelId>();

        public IReadOnlySet<ModelId> BannedPersonaRelicIds => BannedRelicIds;

        public bool EnableExtremeMode { get; init; }

        public bool EnableAllPersonas { get; init; }

        public bool EnableAllVariantPersonas { get; init; }

        public bool EnableDuplicatePersonas { get; init; }

        public bool EnableRandomCloneMode { get; init; }

        public bool EnablePlayRecommendation { get; init; }

        public bool EnableRouteRecommendation { get; init; }

        public bool EnableTokenRecommendation { get; init; }

        public bool EnableAutoPhrase { get; init; }

        public bool EnableTelemetry { get; init; } = true;

        public bool EnableStartupNotifications { get; init; } = true;

        public bool EnableSettingsNotifications { get; init; } = true;

        public bool EnableTelemetryNotifications { get; init; } = true;

        public bool EnableMultiplayerNotifications { get; init; } = true;

        public bool EnablePersonaRelicNotifications { get; init; } = true;

        public bool EnableTokenRelicNotifications { get; init; } = true;

        public bool EnableNeowDiagnosticsNotifications { get; init; } = true;

        public TokenSeriesMode TokenSeriesMode { get; init; } = TokenSeriesMode.RandomTwo;

        public bool EnablePureAngelMode { get; init; } = true;

        public static LocalRuntimeSettings FromPersistent(ReAstralPartyModSettings settings)
        {
            return new LocalRuntimeSettings
            {
                BannedRelicIds = DeserializeModelIdSet(ResolveBannedRelicIds(settings)),
                EnableExtremeMode = settings.EnableExtremeMode,
                EnableAllPersonas = settings.EnableAllPersonas,
                EnableAllVariantPersonas = settings.EnableAllVariantPersonas,
                EnableDuplicatePersonas = settings.EnableDuplicatePersonas,
                EnableRandomCloneMode = settings.EnableRandomCloneMode,
                EnablePlayRecommendation = settings.EnablePlayRecommendation,
                EnableRouteRecommendation = settings.EnableRouteRecommendation,
                EnableTokenRecommendation = settings.EnableTokenRecommendation,
                EnableAutoPhrase = settings.EnableAutoPhrase,
                EnableTelemetry = settings.EnableTelemetry,
                EnableStartupNotifications = settings.EnableStartupNotifications,
                EnableSettingsNotifications = settings.EnableSettingsNotifications,
                EnableTelemetryNotifications = settings.EnableTelemetryNotifications,
                EnableMultiplayerNotifications = settings.EnableMultiplayerNotifications,
                EnablePersonaRelicNotifications = settings.EnablePersonaRelicNotifications,
                EnableTokenRelicNotifications = settings.EnableTokenRelicNotifications,
                EnableNeowDiagnosticsNotifications = settings.EnableNeowDiagnosticsNotifications,
                TokenSeriesMode = ResolveTokenSeriesModeCore(settings),
                EnablePureAngelMode = settings.EnablePureAngelMode
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
        private sealed record RelicListPageState(
            BannedRelicCategory Category,
            ItemList AvailableList,
            ItemList BannedList,
            Label SummaryLabel);

        private readonly IModSettingsUiActionHost _host;
        private readonly Dictionary<BannedRelicCategory, RelicListPageState> _pages = [];
        private readonly Dictionary<BannedRelicCategory, List<RelicModel>> _availableRelicsByCategory = [];
        private readonly Dictionary<BannedRelicCategory, List<RelicModel>> _bannedRelicsByCategory = [];
        private TabContainer? _tabContainer;

        public BannedRelicManagerControl(IModSettingsUiActionHost host)
        {
            _host = host;
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

            _tabContainer = new TabContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            AddChild(_tabContainer);

            foreach (var category in BannedRelicRegistry.Categories)
                _tabContainer.AddChild(BuildCategoryPage(category));

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
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.clear_all").GetRawText()
            });
            ((Button)footer.GetChild(0)).Pressed += () =>
            {
                UpdateBannedRelicIds(Array.Empty<ModelId>());
                RefreshLists();
                NotifyChanged();
            };

            footer.AddChild(new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.reset_default").GetRawText()
            });
            ((Button)footer.GetChild(1)).Pressed += () =>
            {
                UpdateBannedRelicIds(Array.Empty<ModelId>());
                RefreshLists();
                NotifyChanged();
            };

            RefreshLists();
        }

        private Control BuildCategoryPage(BannedRelicCategory category)
        {
            var page = new VBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            page.Name = category.ToString();
            page.AddThemeConstantOverride("separation", 10);

            var summaryLabel = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            page.AddChild(summaryLabel);

            var row = new HBoxContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            row.AddThemeConstantOverride("separation", 12);
            page.AddChild(row);

            row.AddChild(BuildColumn(
                new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.available_title").GetRawText(),
                out var availableList));
            row.AddChild(BuildActionColumn(category));
            row.AddChild(BuildColumn(
                new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.banned_title").GetRawText(),
                out var bannedList));

            availableList.ItemActivated += index => BanSelectedRelic(category, index);
            bannedList.ItemActivated += index => UnbanSelectedRelic(category, index);

            _pages[category] = new RelicListPageState(category, availableList, bannedList, summaryLabel);
            return page;
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

        private VBoxContainer BuildActionColumn(BannedRelicCategory category)
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
            addButton.Pressed += () => BanSelectedRelic(category);
            column.AddChild(addButton);

            var removeButton = new Button
            {
                Text = new LocString("settings_ui",
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.remove_button").GetRawText(),
                CustomMinimumSize = new Vector2(120f, 0f)
            };
            removeButton.Pressed += () => UnbanSelectedRelic(category);
            column.AddChild(removeButton);

            return column;
        }

        private void RefreshLists()
        {
            var bannedIds = BannedRelicIds;
            foreach (var category in BannedRelicRegistry.Categories)
            {
                var allRelics = BannedRelicRegistry.GetCanonicalRelics(category)
                    .OrderBy(GetRelicDisplayName, StringComparer.Ordinal)
                    .ToList();
                var availableRelics = allRelics
                    .Where(relic => !bannedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
                    .ToList();
                var bannedRelics = allRelics
                    .Where(relic => bannedIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
                    .ToList();

                _availableRelicsByCategory[category] = availableRelics;
                _bannedRelicsByCategory[category] = bannedRelics;

                var page = _pages[category];
                RebuildItemList(page.AvailableList, availableRelics);
                RebuildItemList(page.BannedList, bannedRelics);
                page.SummaryLabel.Text = string.Format(
                    new LocString("settings_ui",
                        "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.summary").GetRawText(),
                    bannedRelics.Count,
                    allRelics.Count);
            }

            if (_tabContainer == null)
                return;

            for (var index = 0; index < BannedRelicRegistry.Categories.Count; index++)
            {
                var category = BannedRelicRegistry.Categories[index];
                _tabContainer.SetTabTitle(index, GetCategoryTabTitle(category));
            }
        }

        private static void RebuildItemList(ItemList list, IReadOnlyList<RelicModel> relics)
        {
            list.Clear();
            foreach (var relic in relics)
                list.AddItem(GetRelicDisplayName(relic));
        }

        private void BanSelectedRelic(BannedRelicCategory category)
        {
            var index = _pages[category].AvailableList.GetSelectedItems().FirstOrDefault(-1);
            BanSelectedRelic(category, index);
        }

        private void BanSelectedRelic(BannedRelicCategory category, long index)
        {
            var selectedIndex = (int)index;
            var availableRelics = _availableRelicsByCategory.GetValueOrDefault(category) ?? [];
            if (selectedIndex < 0 || selectedIndex >= availableRelics.Count)
                return;

            var updated = new HashSet<ModelId>(BannedRelicIds)
            {
                availableRelics[selectedIndex].CanonicalInstance?.Id ?? availableRelics[selectedIndex].Id
            };
            UpdateBannedRelicIds(updated);
            RefreshLists();
            NotifyChanged();
        }

        private void UnbanSelectedRelic(BannedRelicCategory category)
        {
            var index = _pages[category].BannedList.GetSelectedItems().FirstOrDefault(-1);
            UnbanSelectedRelic(category, index);
        }

        private void UnbanSelectedRelic(BannedRelicCategory category, long index)
        {
            var selectedIndex = (int)index;
            var bannedRelics = _bannedRelicsByCategory.GetValueOrDefault(category) ?? [];
            if (selectedIndex < 0 || selectedIndex >= bannedRelics.Count)
                return;

            var updated = new HashSet<ModelId>(BannedRelicIds);
            updated.Remove(bannedRelics[selectedIndex].CanonicalInstance?.Id ?? bannedRelics[selectedIndex].Id);
            UpdateBannedRelicIds(updated);
            RefreshLists();
            NotifyChanged();
        }

        private static string GetCategoryTabTitle(BannedRelicCategory category)
        {
            var key = category switch
            {
                BannedRelicCategory.Persona =>
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_persona",
                BannedRelicCategory.VariantPersona =>
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_variant_persona",
                BannedRelicCategory.PersonalityDerivative =>
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_derivative",
                BannedRelicCategory.Token =>
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_token",
                BannedRelicCategory.Other =>
                    "RE_ASTRAL_PARTY_MOD_SETTINGS.banned_personas.category_other",
                _ => category.ToString()
            };
            return new LocString("settings_ui", key).GetRawText();
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
