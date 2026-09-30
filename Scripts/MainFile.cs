using System.Reflection;
using System.Security.Cryptography;
using Color = Godot.Color;
using Mathf = Godot.Mathf;
using GodotTime = Godot.Time;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Ancient;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Tags;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.Server;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Cards.HandOutline;

namespace ReAstralPartyMod;

[ModInitializer(nameof(Init))]
public class MainFile
{
    public const string ModId = "ReAstralPartyMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);
    private static readonly Color TemporaryCardOutlineLow = new(68f / 255f, 55f / 255f, 79f / 255f, 1f);
    private static readonly Color TemporaryCardOutlineHigh = new(250f / 255f, 212f / 255f, 77f / 255f, 1f);
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized)
            return;

        var assembly = Assembly.GetExecutingAssembly();

        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ReAstralPartyModSettingsManager.Register();
        TokenSeriesAvailabilityHelper.Register();
        FinalArenaSeriesHelper.Register();
        IrrefusableCardHelper.Register();
        TerraprismaHelper.Register();
        FallenHeavenlyKingDeckHelper.Register();
        HkHeavenlyKingFallIncidentHelper.Register();
        AstralRelicStore.Register();
        RoyalPrerogativePower.RegisterRightClickBinding();

        // Current content still references minted CardKeyword values while models materialize,
        // so keywords must be registered before assembly auto-discovery runs.
        AstralKeywords.RegisterAll();
        AstralCardTags.RegisterAll();
        StokovStarterBundleHelper.RegisterAll();
        SavedPropertyGovernance.LogGovernanceSummary(assembly);
        SavedPropertyCacheBootstrap.ScheduleVerification(assembly);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        var contentRegistry = RitsuLibFramework.GetContentRegistry(ModId);
        contentRegistry.RegisterCardHandOutline<CardModel>(
            ModCardHandOutlineRules<CardModel>.Dynamic(
                static card => card.Keywords.Contains(AstralKeywords.AstralTemporary),
                static _ => ResolveTemporaryCardOutlineColor(),
                visibleWhenUnplayable: true));
        contentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter<PersonSkillCardPool>(
            "person_skill_pool",
            "res://ReAstralPartyMod/images/ui/person_skill_pool.png");
        contentRegistry.RegisterCardLibraryCompendiumSharedPoolFilter<AstralEventCardPool>(
            "astral_event_card_pool",
            "res://ReAstralPartyMod/images/ui/astral_event_card_pool.png");
        NeowOptionInjectionHelper.Register();
        CombatEntryDiagnostics.Register();
        GameplayPatchRegistry.RegisterAndApply();
        AstralTelemetry.Initialize();
        AstralChoiceProtocol.LogStartupDiagnostics();
        AstralNetPhaseGuard.LogStartupDiagnostics();
        RewardContextPolicy.LogStartupDiagnostics();

        var buildMarker = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString()
                          ?? "unknown";

        _initialized = true;
        Logger.Info($"{ModId} initialized | build={buildMarker}");
        LogLoadedArtifacts(buildMarker);
    }

    private static Color ResolveTemporaryCardOutlineColor()
    {
        var progress = (Mathf.Sin(GodotTime.GetTicksMsec() * MathF.Tau / 2000f) + 1f) * 0.5f;
        return TemporaryCardOutlineLow.Lerp(TemporaryCardOutlineHigh, progress);
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
