using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class StartingPersonaRelicSelectionPatch : IPatchMethod
{
    private const int StartingVariantInjectionChancePercent = 17;
    public static string PatchId => "starting_persona_relic_selection_patch";
    public static bool IsCritical => false;
    public static string Description => "Open the starting persona relic selection after a run starts";

    private static readonly object SelectionLifecycleLock = new();
    private static readonly HashSet<string> ActiveRunKeys = [];
    private static readonly HashSet<string> CompletedRunKeys = [];

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NGame), "StartRun", [typeof(RunState)])];
    }

    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterStartRun(__result, runState);
    }

    private static async Task RunAfterStartRun(Task originalTask, RunState runState)
    {
        await originalTask;
        LogInfo("P002",
            $"Starting persona relic selection patch entered: seed={runState.Rng.StringSeed} players={runState.Players.Count}.");
        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.StartRunBootstrap, "starting persona selection bootstrap"))
        {
            LogWarn("P003",
                "Starting persona relic selection bootstrap was not ready yet; waiting briefly instead of skipping.");
            await WaitForBootstrapReadinessAsync();
        }

        LogInfo("P004", "Starting persona relic selection patch resumed after StartRun task completed.");
        LogInfo("P005", "Starting persona relic selection waiting for run settings sync.");
        await ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);
        LogInfo("P006", "Starting persona relic selection finished run settings sync.");
        var gameType = RunManager.Instance.NetService.Type;
        LogInfo("P007",
            $"Starting persona relic selection run gate: netMode={gameType} players={runState.Players.Count}.");
        if (!ShouldOpenStartingPersonaRelicSelection(runState, out var skipReason))
        {
            LogInfo("P008", $"Starting persona relic selection skipped: {skipReason}.");
            if (skipReason.Contains("already own persona relics", StringComparison.Ordinal))
            {
                ShowWarning("P008", "启动门禁", "检测到至少一名玩家在开局阶段已经持有人格遗物，因此本轮人格选择被跳过。请反馈编号和日志。");
            }
            return;
        }

        var runKey = GetRunKey(runState);
        if (!TryBeginSelection(runKey))
        {
            LogInfo("P009",
                $"Starting persona relic selection skipped because run '{runKey}' is already processing.");
            return;
        }

        LogInfo("P010", "Starting persona relic selection waiting for overlay stack.");
        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
        {
            EndSelection(runKey, false);
            LogWarn("P011", "Starting persona relic selection skipped because overlay stack is not ready.");
            ShowWarning("P011", "界面打开", "未能拿到人格选择覆盖层，界面没有正常打开。请反馈编号。");
            return;
        }

        var relicOptions = CreateStartingPersonaRelicOptions(runState);
        if (relicOptions.Count == 0)
        {
            EndSelection(runKey, false);
            LogWarn("P012", "Starting persona relic selection skipped because no persona relics are registered.");
            ShowWarning("P012", "选项构建", "未找到可用的人格选项。请反馈编号和日志。");
            return;
        }

        if (ReAstralPartyModSettingsManager.GetEnableRandomCloneMode(runState))
        {
            try
            {
                var sharedRelic = ChooseSharedRandomClonePersona(runState, relicOptions);
                var sharedIndex = relicOptions
                    .Select((relic, index) => new { relic, index })
                    .First(entry => entry.relic.Id == sharedRelic.Id)
                    .index;
                MainFile.Logger.Info(
                    $"Starting persona random clone mode applied: runKey={runKey} persona={sharedRelic.Id.Entry} players={runState.Players.Count}.");

                foreach (var player in runState.Players.OrderBy(static player => player.NetId))
                    await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(player, sharedRelic);

                var selectedIndexes = runState.Players
                    .ToDictionary(
                        static player => player.NetId,
                        _ => sharedIndex);
                AstralTelemetry.RecordPersonaChoice(runState, relicOptions, selectedIndexes);
                LogInfo("P013",
                    $"Starting persona random clone mode applied: runKey={runKey} persona={sharedRelic.Id.Entry} players={runState.Players.Count}.");
                AstralNotificationService.ShowDiagnosticInfo(
                    AstralNotificationModule.Multiplayer,
                    AstralNotificationArea.PersonaSelection,
                    13,
                    $"本局统一人格：{sharedRelic.Title.GetFormattedText()}",
                    "随机克隆模式");
                EndSelection(runKey, true);
                return;
            }
            catch
            {
                EndSelection(runKey, false);
                throw;
            }
        }

        LogInfo("P014",
            $"Starting persona relic selection options prepared: count={relicOptions.Count} runKey={runKey}.");
        var screen = StartingPersonaRelicSelectionScreen.Create(runState, relicOptions);
        try
        {
            overlayStack.Push(screen);
            LogInfo("P015",
                $"Starting persona relic selection shown with {relicOptions.Count} persona relic options.");
            await screen.RelicPickingFinished();
            LogInfo("P016", "Starting persona relic selection screen completed.");
            EndSelection(runKey, true);
        }
        catch
        {
            EndSelection(runKey, false);
            throw;
        }
        finally
        {
            screen.Close();
        }
    }

    private static void LogInfo(string code, string message)
    {
        MainFile.Logger.Info($"[{code}] {message}");
    }

    private static void LogWarn(string code, string message)
    {
        MainFile.Logger.Warn($"[{code}] {message}");
    }

    private static void ShowWarning(string code, string stage, string body)
    {
        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.PersonaSelection,
            ParseCodeNumber(code),
            body,
            stage);
    }

    private static async Task WaitForBootstrapReadinessAsync()
    {
        for (var attempt = 0; attempt < 180; attempt++)
        {
            if (RunManager.Instance?.DebugOnlyGetState() != null)
                return;

            if (NGame.Instance != null && NOverlayStack.Instance != null)
                return;

            await Task.Yield();
        }
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            var overlayStack = NOverlayStack.Instance;
            if (overlayStack != null && Godot.GodotObject.IsInstanceValid(overlayStack))
                return overlayStack;

            await Task.Yield();
        }

        return null;
    }

    private static bool ShouldOpenStartingPersonaRelicSelection(RunState runState, out string reason)
    {
        var existingPersonaOwners = runState.Players
            .Where(player => player.Relics.Any(IsOwnedPersonaRelic))
            .Select(player => player.NetId)
            .ToList();
        if (existingPersonaOwners.Count > 0)
        {
            reason = $"players already own persona relics ({string.Join(", ", existingPersonaOwners)})";
            return false;
        }

        reason = "fresh run without persona relics detected";
        return true;
    }

    private static bool IsOwnedPersonaRelic(RelicModel relic)
    {
        return PersonaRelicRegistry.IsPersonaRelic(relic.CanonicalInstance ?? relic);
    }

    private static IReadOnlyList<RelicModel> CreateStartingPersonaRelicOptions(RunState runState)
    {
        var bannedPersonaRelicIds = ReAstralPartyModSettingsManager.GetBannedPersonaRelicIds(runState);
        var allPersonaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelicsFiltered(bannedPersonaRelicIds)
            .OrderBy(relic => relic.Id.Entry)
            .ToList();

        if (allPersonaRelics.Count == 0)
        {
            MainFile.Logger.Warn("ban list filtered all persona options; falling back to the full persona pool.");
            allPersonaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics()
                .OrderBy(relic => relic.Id.Entry)
                .ToList();
        }

        var ownedPersonaRelicIds = runState.Players
            .SelectMany(player => player.Relics)
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

        if (ReAstralPartyModSettingsManager.GetEnableAllPersonas(runState))
        {
            var allAvailableOptions = allPersonaRelics
                .Where(relic => !ownedPersonaRelicIds.Contains(relic.Id))
                .ToList();

            var allModeOptions = allAvailableOptions.Count > 0 ? allAvailableOptions : allPersonaRelics;
            return ApplyStartingVariantPersonaPostProcessing(runState, allModeOptions);
        }

        var targetCount = runState.Players.Count * 2 + 2;
        var weightedCandidates = ExpandWeightedStartingPersonaCandidates(
            allPersonaRelics
                .Where(relic => !ownedPersonaRelicIds.Contains(relic.Id))
                .ToList());
        var options = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                weightedCandidates,
                relic => relic.Id.Entry,
                MainFile.ModId,
                "starting_persona_primary_pool",
                runState.Rng.StringSeed,
                runState.Players.Count)
            .DistinctBy(relic => relic.Id)
            .Take(targetCount)
            .ToList();

        if (options.Count < targetCount)
        {
            LogInfo("P025",
                $"Starting persona primary pool collapsed duplicate weighted rolls: uniqueOptions={options.Count} targetCount={targetCount} weightedCandidates={weightedCandidates.Count}.");
        }

        if (options.Count >= targetCount)
            return ApplyStartingVariantPersonaPostProcessing(runState, options);

        var selectedIds = options.Select(relic => relic.Id).ToHashSet();
        var fallbackOptions = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            allPersonaRelics
                .Where(relic => !selectedIds.Contains(relic.Id))
                .ToList(),
            relic => relic.Id.Entry,
            MainFile.ModId,
            "starting_persona_fallback_pool",
            runState.Rng.StringSeed,
            runState.Players.Count);

        options.AddRange(fallbackOptions.Take(targetCount - options.Count));
        return ApplyStartingVariantPersonaPostProcessing(runState, options);
    }

    private static List<RelicModel> ExpandWeightedStartingPersonaCandidates(IReadOnlyList<RelicModel> source)
    {
        var weighted = new List<RelicModel>();
        foreach (var relic in source)
        {
            var weight = IsWindchaserVariantRelic(relic) ? 1 : 6;
            for (var i = 0; i < weight; i++)
                weighted.Add(relic);
        }

        return weighted;
    }

    private static IReadOnlyList<RelicModel> AddForcedWindchaserVariantIfNeeded(
        RunState runState,
        IReadOnlyList<RelicModel> source)
    {
        if (!CompatContentGate.ShouldForceStartingVariantPersonaForRun(
                new CompatContentGate.RunStateLike(runState.Players)))
            return source;

        var forcedRelic = PersonaRelicRegistry.GetCanonicalVariantPersonaRelics()
            .FirstOrDefault(IsWindchaserVariantRelic);
        if (forcedRelic == null)
            return source;
        if (source.Any(relic => relic.Id == forcedRelic.Id))
            return source;

        var result = source.ToList();
        result.Add(forcedRelic);
        return result;
    }

    private static IReadOnlyList<RelicModel> ApplyStartingVariantPersonaPostProcessing(
        RunState runState,
        IReadOnlyList<RelicModel> source)
    {
        var options = AddForcedWindchaserVariantIfNeeded(runState, source).ToList();
        if (ReAstralPartyModSettingsManager.GetEnableAllVariantPersonas(runState))
            return AddAllBuiltInVariantPersonas(runState, options);

        return AddChainedVariantPersonasIfNeeded(runState, options);
    }

    private static IReadOnlyList<RelicModel> AddAllBuiltInVariantPersonas(
        RunState runState,
        List<RelicModel> options)
    {
        var builtInVariants = PersonaRelicRegistry.GetStartingBuiltInVariantPersonaRelics()
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (builtInVariants.Count == 0)
        {
            LogInfo("P023", "Starting persona all-variant mode skipped because no built-in variants are registered.");
            return options;
        }

        var appendedCount = 0;
        foreach (var variant in builtInVariants)
        {
            if (options.Any(existing => existing.Id == variant.Id))
                continue;

            options.Add(variant);
            appendedCount++;
        }

        LogInfo("P024",
            $"Starting persona all-variant mode applied: appended={appendedCount} totalOptions={options.Count}.");
        return options;
    }

    private static IReadOnlyList<RelicModel> AddChainedVariantPersonasIfNeeded(
        RunState runState,
        List<RelicModel> options)
    {
        var allVariants = PersonaRelicRegistry.GetStartingBuiltInVariantPersonaRelics()
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (allVariants.Count == 0)
        {
            LogInfo("P017", "Starting persona variant chain skipped because no gameplay-available variants are registered.");
            return options;
        }

        LogInfo("P018",
            $"Starting persona variant chain started: initialOptions={options.Count} variantPool={allVariants.Count}.");

        var appendedCount = 0;
        foreach (var variant in allVariants)
        {
            if (options.Any(existing => existing.Id == variant.Id))
            {
                LogInfo("P019",
                    $"Starting persona variant independent roll skipped because variant is already present: relic={variant.Id.Entry}.");
                continue;
            }

            var shouldInject = ShouldInjectStartingVariantPersona(variant, runState);
            LogInfo("P020",
                $"Starting persona variant independent roll: relic={variant.Id.Entry} hit={shouldInject} chance={StartingVariantInjectionChancePercent}%.");
            if (!shouldInject)
                continue;

            options.Add(variant);
            appendedCount++;
            LogInfo("P021",
                $"Starting persona variant appended: relic={variant.Id.Entry} totalOptions={options.Count}.");
        }

        LogInfo("P022",
            $"Starting persona variant chain completed: totalOptions={options.Count} appended={appendedCount} variantCount={options.Count(PersonaRelicRegistry.IsVariantPersonaRelic)}.");
        return options;
    }

    private static bool ShouldInjectStartingVariantPersona(
        RelicModel variant,
        RunState runState)
    {
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            MainFile.ModId,
            "starting_variant_independent_roll",
            runState.Rng.StringSeed,
            runState.Players.Count,
            variant.Id.Entry);
        return roll < StartingVariantInjectionChancePercent;
    }

    private static bool IsWindchaserVariantRelic(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic) is VariantPersonWindchaserThePlaneswalker;
    }

    private static RelicModel ChooseSharedRandomClonePersona(RunState runState, IReadOnlyList<RelicModel> options)
    {
        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            options.Count,
            MainFile.ModId,
            "starting_persona_random_clone_mode",
            runState.Rng.StringSeed,
            runState.Players.Count);
        return options[selectedIndex];
    }

    private static string GetRunKey(RunState runState)
    {
        var orderedPlayers = runState.Players
            .Select(player => player.NetId.ToString())
            .OrderBy(static netId => netId, StringComparer.Ordinal);
        return $"{runState.Rng.StringSeed}|{string.Join(",", orderedPlayers)}";
    }

    private static bool TryBeginSelection(string runKey)
    {
        lock (SelectionLifecycleLock)
        {
            if (CompletedRunKeys.Contains(runKey) || ActiveRunKeys.Contains(runKey))
                return false;

            ActiveRunKeys.Add(runKey);
            return true;
        }
    }

    private static void EndSelection(string runKey, bool completed)
    {
        lock (SelectionLifecycleLock)
        {
            ActiveRunKeys.Remove(runKey);
            if (completed)
                CompletedRunKeys.Add(runKey);
        }
    }

    private static int ParseCodeNumber(string code)
    {
        return int.TryParse(code.AsSpan(1), out var number) ? number : 0;
    }
}
