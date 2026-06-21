using System.Reflection;
using System.Security.Cryptography;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using ReAstralPartyMod.ReAstralPartyCardCode.Ancient;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib;
using STS2RitsuLib.Interop;

namespace ReAstralPartyMod;

[ModInitializer(nameof(Init))]
public class MainFile
{
    public const string ModId = "ReAstralPartyMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized)
            return;

        var assembly = Assembly.GetExecutingAssembly();

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ReAstralPartyModSettingsManager.Register();

        // Current content still references minted CardKeyword values while models materialize,
        // so keywords must be registered before assembly auto-discovery runs.
        AstralKeywords.RegisterAll();
        AstralCardTags.RegisterAll();
        EnigmaticRewardRegistry.RegisterAll();
        StokovStarterBundleHelper.RegisterAll();
        SavedPropertyGovernance.LogGovernanceSummary(assembly);
        SavedPropertyCacheBootstrap.ScheduleVerification(assembly);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        var contentRegistry = RitsuLibFramework.GetContentRegistry(ModId);
        contentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter<PersonSkillCardPool>(
            "person_skill_pool",
            "res://ReAstralPartyMod/images/ui/person_skill_pool.png");
        contentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter<AstralEventCardPool>(
            "astral_event_card_pool",
            "res://ReAstralPartyMod/images/ui/astral_event_card_pool.png");
        NeowOptionInjectionHelper.Register();
        GameplayPatchRegistry.RegisterAndApply();
        AstralTelemetry.Initialize();
        LucidDreamMaliceModifierInstaller.RegisterLifecycleBridgeIfNeeded();
        AstralChoiceProtocol.LogStartupDiagnostics();
        AstralNetPhaseGuard.LogStartupDiagnostics();
        RewardContextPolicy.LogStartupDiagnostics();
        EnigmaticUniqueMaterialRewardSync.Register();

        var buildMarker = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "unknown";

        _initialized = true;
        Logger.Info($"{ModId} initialized | build={buildMarker}");
        LogLoadedArtifacts(buildMarker);
    }

    private static void LogLoadedArtifacts(string buildMarker)
    {
        try
        {
            var assemblyPath = typeof(MainFile).Assembly.Location;
            var modDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            var pckPath = Path.Combine(modDirectory, $"{ModId}.pck");
            var manifestPath = Path.Combine(modDirectory, $"{ModId}.json");

            Logger.Info(
                $"{ModId} artifacts | build={buildMarker} | dll={assemblyPath} | dll_sha256={ComputeSha256(assemblyPath)}");

            if (File.Exists(pckPath))
                Logger.Info($"{ModId} artifacts | pck={pckPath} | pck_sha256={ComputeSha256(pckPath)}");
            else
                Logger.Warn($"{ModId} artifacts | pck missing at {pckPath}");

            if (File.Exists(manifestPath))
                Logger.Info(
                    $"{ModId} artifacts | manifest={manifestPath} | manifest_sha256={ComputeSha256(manifestPath)}");
            else
                Logger.Warn($"{ModId} artifacts | manifest missing at {manifestPath}");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to log {ModId} artifact hashes: {ex}");
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}
