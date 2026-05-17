using STS2RitsuLib;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public enum TokenSeriesMode
{
    RandomTwo = 0,
    All = 1,
    Disabled = 2
}

public sealed class ReAstralPartyModSettings
{
    public bool EnableAllPersonas { get; set; }

    public bool EnableDuplicatePersonas { get; set; }

    public bool EnableTelemetry { get; set; } = true;

    // Legacy bool setting kept for backward compatibility with older settings.json files.
    public bool? EnableAllTokenSeries { get; set; }

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnablePureAngelMode { get; set; } = true;
}

public static class ReAstralPartyModSettingsManager
{
    public const string SettingsKey = "settings";

    private static bool _registered;
    private static bool _loggedMultiplayerFallback;

    public static bool EnableAllPersonas => Read(settings => settings.EnableAllPersonas);

    public static bool EnableDuplicatePersonas => Read(settings => settings.EnableDuplicatePersonas);

    public static TokenSeriesMode TokenSeriesMode => ReadTokenSeriesMode();

    public static bool EnablePureAngelMode => Read(settings => settings.EnablePureAngelMode);

    public static bool EnableTelemetry => ReadTelemetry();

    public static ReAstralPartyModSettings ReadLocalSettings()
    {
        return RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey);
    }

    public static TokenSeriesMode ResolveTokenSeriesMode(ReAstralPartyModSettings settings)
    {
        return GetTokenSeriesMode(settings);
    }

    public static bool TryGetRunSnapshot(IRunState? runState, out ReAstralPartyRunSettingsSnapshot snapshot)
    {
        return ReAstralPartyRunSettingsSync.TryGetSnapshot(runState, out snapshot);
    }

    public static bool GetEnableAllPersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableAllPersonas;

        return EnableAllPersonas;
    }

    public static bool GetEnableDuplicatePersonas(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnableDuplicatePersonas;

        return EnableDuplicatePersonas;
    }

    public static TokenSeriesMode GetTokenSeriesMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.TokenSeriesMode;

        return TokenSeriesMode;
    }

    public static bool GetEnablePureAngelMode(IRunState? runState)
    {
        if (TryGetRunSnapshot(runState, out var snapshot))
            return snapshot.EnablePureAngelMode;

        return EnablePureAngelMode;
    }

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            var store = RitsuLibFramework.GetDataStore(MainFile.ModId);
            store.Register<ReAstralPartyModSettings>(
                key: SettingsKey,
                fileName: "settings.json",
                scope: SaveScope.Global,
                defaultFactory: static () => new ReAstralPartyModSettings(),
                autoCreateIfMissing: true);
        }

        RegisterSettingsPage();
        _registered = true;
        MainFile.Logger.Info($"{MainFile.ModId} mod settings registered.");
    }

    internal static void SetEnableTelemetry(bool enabled)
    {
        var settings = RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey);
        settings.EnableTelemetry = enabled;
    }

    private static bool Read(Func<ReAstralPartyModSettings, bool> selector)
    {
        if (!ShouldUseLocalSettings())
            return false;

        try
        {
            return selector(RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey));
        }
        catch
        {
            return false;
        }
    }

    private static bool ReadTelemetry()
    {
        try
        {
            return RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey)
                .EnableTelemetry;
        }
        catch
        {
            return true;
        }
    }

    private static void RegisterSettingsPage()
    {
        var enableAllPersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableAllPersonas,
            (settings, value) => settings.EnableAllPersonas = value);

        var enableDuplicatePersonas = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableDuplicatePersonas,
            (settings, value) => settings.EnableDuplicatePersonas = value);

        var tokenSeriesMode = ModSettingsBindings.Global<ReAstralPartyModSettings, TokenSeriesMode>(
            MainFile.ModId,
            SettingsKey,
            settings => GetTokenSeriesMode(settings),
            (settings, value) =>
            {
                settings.TokenSeriesMode = value;
                settings.EnableAllTokenSeries = null;
            });

        var enablePureAngelMode = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnablePureAngelMode,
            (settings, value) => settings.EnablePureAngelMode = value);

        var enableTelemetry = ModSettingsBindings.Global<ReAstralPartyModSettings, bool>(
            MainFile.ModId,
            SettingsKey,
            settings => settings.EnableTelemetry,
            (settings, value) =>
            {
                settings.EnableTelemetry = value;
                ReAstralPartyCardCode.Online.AstralTelemetry.SetCollectionEnabledByConsent(value);
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
                    "enable_duplicate_personas",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_duplicate_personas.label", "Enable Duplicate Personas"),
                    enableDuplicatePersonas,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_duplicate_personas.description",
                        "Multiple players can choose the same starting persona without conflict."))
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
                .AddToggle(
                    "enable_pure_angel_mode",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_pure_angel_mode.label", "Enable Pure Angel Mode"),
                    enablePureAngelMode,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_pure_angel_mode.description",
                        "Reserved toggle. It currently has no gameplay effect.")))
            .AddSection("telemetry", section => section
                .WithTitle(T("RE_ASTRAL_PARTY_MOD_SETTINGS.telemetry.title", "Telemetry"))
                .WithDescription(T("RE_ASTRAL_PARTY_MOD_SETTINGS.telemetry.description",
                    "Manage anonymous balance telemetry at any time."))
                .AddToggle(
                    "enable_telemetry",
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.label", "Enable Anonymous Telemetry"),
                    enableTelemetry,
                    T("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_telemetry.description",
                        "Allow anonymous usage statistics for personas, tokens, and skill cards."))));
    }

    private static ModSettingsText T(string key, string fallback)
    {
        return ModSettingsText.LocString("settings_ui", key, fallback);
    }

    private static TokenSeriesMode ReadTokenSeriesMode()
    {
        if (!ShouldUseLocalSettings())
            return TokenSeriesMode.RandomTwo;

        try
        {
            var settings = RitsuLibFramework.GetDataStore(MainFile.ModId).Get<ReAstralPartyModSettings>(SettingsKey);
            return GetTokenSeriesMode(settings);
        }
        catch
        {
            return TokenSeriesMode.RandomTwo;
        }
    }

    private static TokenSeriesMode GetTokenSeriesMode(ReAstralPartyModSettings settings)
    {
        if (settings.EnableAllTokenSeries.HasValue)
            return settings.EnableAllTokenSeries.Value ? TokenSeriesMode.All : TokenSeriesMode.RandomTwo;

        return settings.TokenSeriesMode;
    }

    private static bool ShouldUseLocalSettings()
    {
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (netService == null)
            return true;

        if (netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return true;

        if (!_loggedMultiplayerFallback)
        {
            _loggedMultiplayerFallback = true;
            MainFile.Logger.Warn(
                $"{MainFile.ModId} mod settings fallback: multiplayer detected, using synchronized-safe defaults for persona/token-series options.");
        }

        return false;
    }
}
