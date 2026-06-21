using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.ManosabaLin;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class StartingPersonRelicSelectionPatch : IPatchMethod
{
    private const int StartingVariantInjectionChancePercent = 17;
    private const int AutomaticSelectionCountdownSeconds = 5;
    private const decimal StartingInitialPointGoldCost = 1m;
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
        LobbyGameplaySettingsSync.MarkRunStarting();
        StartingPersonHostLaunchSync.Register();
        DevConsoleCommandNotificationSync.Register();
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
        LucidDreamMaliceModifierInstaller.EnsureInstalledForNewRun(runState);
        await GrantStartingInitialPointIfEnabledAsync(runState);
        await GrantStartingRingOfSevenCursesIfEnabledAsync(runState);
        LogInfo("P007", "Starting persona relic selection run bootstrap completed; Neow ready-page flow will handle the actual persona selection entry.");
    }

    internal static async Task<bool> OpenSelectionOverlayAsync(
        RunState runState,
        string sourceTag,
        IReadOnlyList<RelicModel>? overrideRelicOptions = null)
    {
        if (!ShouldOpenStartingPersonaRelicSelection(runState, out var skipReason))
        {
            LogInfo("P008", $"Starting persona relic selection skipped from {sourceTag}: {skipReason}.");
            if (skipReason.Contains("already own persona relics", StringComparison.Ordinal))
            {
                ShowWarning("P008", "启动门禁", "检测到至少一名玩家在开局阶段已经持有人格遗物，因此本轮人格选择被跳过。请反馈编号和日志。");
            }

            return false;
        }

        var runKey = GetRunKey(runState);
        if (!TryBeginSelection(runKey))
        {
            LogInfo("P009",
                $"Starting persona relic selection skipped from {sourceTag} because run '{runKey}' is already processing.");
            return false;
        }

        LogInfo("P010", "Starting persona relic selection waiting for overlay stack.");
        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
        {
            EndSelection(runKey, false);
            LogWarn("P011", "Starting persona relic selection skipped because overlay stack is not ready.");
            ShowWarning("P011", "界面打开", "未能拿到人格选择覆盖层，界面没有正常打开。请反馈编号。");
            return false;
        }

        var displayMode = ReAstralPartyModSettingsManager.GetStartingPersonaDisplayMode(runState);
        var assignmentMode = ReAstralPartyModSettingsManager.GetStartingPersonaAssignmentMode(runState);
        var relicOptions = overrideRelicOptions?.Count > 0
            ? overrideRelicOptions
            : CreateOverlayRelicOptions(runState);
        if (relicOptions.Count == 0)
        {
            EndSelection(runKey, false);
            LogWarn("P012", "Starting persona relic selection skipped because no persona relics are registered.");
            ShowWarning("P012", "选项构建", "未找到可用的人格选项。请反馈编号和日志。");
            return false;
        }

        LogInfo("P014",
            $"Starting persona selection overlay prepared from {sourceTag}: count={relicOptions.Count} runKey={runKey} displayMode={displayMode} assignmentMode={assignmentMode}.");
        var screen = StartingPersonRelicSelectionScreen.Create(
            runState,
            relicOptions,
            displayMode,
            assignmentMode,
            AutomaticSelectionCountdownSeconds);
        try
        {
            overlayStack.Push(screen);
            LogInfo("P015",
                $"Starting persona relic selection shown with {relicOptions.Count} persona relic options.");
            await screen.RelicPickingFinished();
            LogInfo("P016A", "Starting persona overlay result completed.");
            LogInfo("P016B", "Starting persona overlay close requested.");
            screen.Close();
            await screen.WaitUntilClosedAsync();
            LogInfo("P016C", "Starting persona overlay actually closed.");
            LogInfo("P016", "Starting persona relic selection screen completed.");
            AstralNeowDiagnosticHelper.ReportPostPersonaSelectionWindow(runState, relicOptions.Count);
            EndSelection(runKey, true);
            return true;
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

            await WaitForNextProcessFrameAsync();
        }
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var attempt = 0; attempt < 120; attempt++)
        {
            var overlayStack = NOverlayStack.Instance;
            if (overlayStack != null && Godot.GodotObject.IsInstanceValid(overlayStack))
                return overlayStack;

            await WaitForNextProcessFrameAsync();
        }

        return null;
    }

    private static async Task WaitForNextProcessFrameAsync()
    {
        if (NGame.Instance?.IsInsideTree() == true)
        {
            await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            return;
        }

        await Task.Yield();
    }

    internal static bool ShouldOpenStartingPersonaRelicSelection(RunState runState, out string reason)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableStartingPersonaSelection(runState))
        {
            reason = "starting persona selection disabled by gameplay settings";
            return false;
        }

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

    private static async Task GrantStartingInitialPointIfEnabledAsync(RunState runState)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableStartingInitialPoint(runState))
            return;

        foreach (var player in runState.Players)
        {
            if (player.Gold < StartingInitialPointGoldCost)
            {
                LogWarn("P006A",
                    $"Starting Initial Point skipped for player {player.NetId} because gold={player.Gold} is below cost={StartingInitialPointGoldCost}.");
                continue;
            }

            LogInfo("P006B",
                $"Granting Starting Initial Point to player {player.NetId} | gold_before={player.Gold}.");
            await PersonMultiplayerEffectHelper.LoseGoldDeterministic(
                StartingInitialPointGoldCost,
                player,
                GoldLossType.Spent);
            await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(player, ModelDb.Relic<TokenGoldInitialPoint>());
            LogInfo("P006C",
                $"Granted Starting Initial Point to player {player.NetId} | gold_after={player.Gold}.");
        }
    }

    private static async Task GrantStartingRingOfSevenCursesIfEnabledAsync(RunState runState)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableStartingRingOfSevenCurses(runState))
            return;

        foreach (var player in runState.Players)
        {
            LogInfo("P006D",
                $"Granting Ring of Seven Curses to player {player.NetId}.");
            await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(player, ModelDb.Relic<EnigmaticSevenCurses>());
        }
    }

    private static bool IsOwnedPersonaRelic(RelicModel relic)
    {
        return PersonRelicRegistry.IsPersonaRelic(relic.CanonicalInstance ?? relic);
    }

    internal static IReadOnlyList<RelicModel> CreateOverlayRelicOptions(RunState runState)
    {
        var displayMode = ReAstralPartyModSettingsManager.GetStartingPersonaDisplayMode(runState);
        return displayMode == StartingPersonDisplayMode.Automatic
            ? CreateAutomaticStartingPersonaRelicOptions(runState, runState.Players.Count)
            : CreateStartingPersonaRelicOptions(runState);
    }

    internal static IReadOnlyList<RelicModel> ResolveRelicOptionsFromSerializedIds(
        IEnumerable<string>? serializedRelicIds,
        string sourceTag)
    {
        if (serializedRelicIds == null)
            return [];

        var resolved = new List<RelicModel>();
        var seenIds = new HashSet<ModelId>();
        foreach (var serializedId in serializedRelicIds)
        {
            if (string.IsNullOrWhiteSpace(serializedId))
                continue;

            try
            {
                var relicId = ModelId.Deserialize(serializedId);
                if (relicId == ModelId.none || !seenIds.Add(relicId))
                    continue;

                resolved.Add(ModelDb.GetById<RelicModel>(relicId));
            }
            catch (Exception ex)
            {
                LogWarn("P014A",
                    $"Starting persona overlay skipped invalid serialized relic id from {sourceTag}: '{serializedId}'. {ex.Message}");
            }
        }

        return resolved;
    }

    private static IReadOnlyList<RelicModel> CreateStartingPersonaRelicOptions(RunState runState)
    {
        var bannedPersonaRelicIds = ReAstralPartyModSettingsManager.GetBannedPersonaRelicIds(runState);
        var allPersonaRelics = PersonRelicRegistry.GetCanonicalPersonaRelicsFiltered(bannedPersonaRelicIds)
            .OrderBy(relic => relic.Id.Entry)
            .ToList();

        if (allPersonaRelics.Count == 0)
        {
            MainFile.Logger.Warn("ban list filtered all persona options; falling back to the full persona pool.");
            allPersonaRelics = PersonRelicRegistry.GetCanonicalPersonaRelics()
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

    private static IReadOnlyList<RelicModel> CreateAutomaticStartingPersonaRelicOptions(RunState runState, int playerCount)
    {
        var pool = CreateAutomaticStartingPersonaPool(runState);
        if (pool.Count == 0)
            return [];

        var uniquePool = new List<RelicModel>();
        var uniquePoolIds = new HashSet<ModelId>();
        foreach (var relic in pool)
        {
            var canonicalId = relic.CanonicalInstance?.Id ?? relic.Id;
            if (!uniquePoolIds.Add(canonicalId))
                continue;

            uniquePool.Add(relic);
        }

        if (ReAstralPartyModSettingsManager.GetEnableAllPersonas(runState))
        {
            LogInfo("P027",
                $"Starting persona automatic all-personas mode applied: uniqueOptions={uniquePool.Count} players={playerCount}.");
            return uniquePool;
        }

        var targetCount = Math.Max(playerCount * 2 + 2, playerCount);
        var options = new List<RelicModel>(targetCount);
        var selectedIds = new HashSet<ModelId>();

        foreach (var relic in pool)
        {
            var canonicalId = relic.CanonicalInstance?.Id ?? relic.Id;
            if (!selectedIds.Add(canonicalId))
                continue;

            options.Add(relic);
            if (options.Count >= targetCount)
                break;
        }

        if (options.Count >= targetCount)
            return options;

        LogWarn("P026",
            $"Starting persona automatic mode had fewer unique options than target count: unique={options.Count} target={targetCount}. Deterministic duplicates will be used to fill the screen.");

        for (var i = 0; options.Count < targetCount; i++)
            options.Add(pool[i % pool.Count]);

        return options;
    }

    private static List<RelicModel> CreateAutomaticStartingPersonaPool(RunState runState)
    {
        var bannedPersonaRelicIds = ReAstralPartyModSettingsManager.GetBannedPersonaRelicIds(runState);
        var ownedPersonaRelicIds = runState.Players
            .SelectMany(player => player.Relics)
            .Select(relic => relic.CanonicalInstance?.Id ?? relic.Id)
            .ToHashSet();

        var pool = PersonRelicRegistry.GetCanonicalPersonaRelicsFiltered(bannedPersonaRelicIds)
            .Where(relic => !ownedPersonaRelicIds.Contains(relic.CanonicalInstance?.Id ?? relic.Id))
            .ToList();
        if (pool.Count == 0)
        {
            pool = PersonRelicRegistry.GetCanonicalPersonaRelicsFiltered(bannedPersonaRelicIds)
                .ToList();
        }

        if (!ReAstralPartyModSettingsManager.GetEnableVariantPersonas(runState))
        {
            return DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                pool,
                relic => relic.Id.Entry,
                MainFile.ModId,
                "starting_persona_automatic_pool",
                runState.Rng.StringSeed,
                runState.Players.Count)
                .ToList();
        }

        if (ReAstralPartyModSettingsManager.GetEnableAllVariantPersonas(runState))
        {
            var bannedRelicIds = ReAstralPartyModSettingsManager.GetBannedRelicIds(runState);
            foreach (var variant in PersonRelicRegistry.GetStartingBuiltInVariantPersonaRelics())
            {
                if (BannedRelicRegistry.IsBanned(bannedRelicIds, variant))
                    continue;
                if (pool.Any(existing => existing.Id == variant.Id))
                    continue;

                pool.Add(variant);
            }
        }

        pool = AddForcedCompatVariantsIfNeeded(runState, pool)
            .ToList();

        return DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            pool,
            relic => relic.Id.Entry,
            MainFile.ModId,
            "starting_persona_automatic_pool",
            runState.Rng.StringSeed,
            runState.Players.Count)
            .ToList();
    }

    private static List<RelicModel> ExpandWeightedStartingPersonaCandidates(IReadOnlyList<RelicModel> source)
    {
        var weighted = new List<RelicModel>();
        foreach (var relic in source)
        {
            var weight = IsForcedCompatVariantRelic(relic) ? 1 : 6;
            for (var i = 0; i < weight; i++)
                weighted.Add(relic);
        }

        return weighted;
    }

    private static IReadOnlyList<RelicModel> AddForcedCompatVariantsIfNeeded(
        RunState runState,
        IReadOnlyList<RelicModel> source)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableVariantPersonas(runState))
            return source;

        if (!CompatContentGate.ShouldForceStartingVariantPersonaForRun(
                new CompatContentGate.RunStateLike(runState.Players)))
            return source;

        var result = source.ToList();
        var bannedRelicIds = ReAstralPartyModSettingsManager.GetBannedRelicIds(runState);
        var forcedRelics = PersonRelicRegistry.GetCanonicalVariantPersonaRelics()
            .Where(relic => !BannedRelicRegistry.IsBanned(bannedRelicIds, relic))
            .Where(IsForcedCompatVariantRelic)
            .Where(relic =>
            {
                var canonical = relic.CanonicalInstance ?? relic;
                if (canonical is VariantPersonWindchaserThePlaneswalker)
                    return WindchaserCompat.IsLoaded() && runState.Players.Any(WindchaserCompat.IsCharacter);
                if (canonical is VariantPersonManosabaLinHiro)
                    return ManosabaLinCompat.IsLoaded() && runState.Players.Any(ManosabaLinCompat.IsCharacter);

                return false;
            })
            .ToList();

        foreach (var forcedRelic in forcedRelics)
        {
            if (result.Any(relic => relic.Id == forcedRelic.Id))
                continue;

            result.Add(forcedRelic);
        }

        return result;
    }

    private static IReadOnlyList<RelicModel> ApplyStartingVariantPersonaPostProcessing(
        RunState runState,
        IReadOnlyList<RelicModel> source)
    {
        if (!ReAstralPartyModSettingsManager.GetEnableVariantPersonas(runState))
            return source;

        var options = AddForcedCompatVariantsIfNeeded(runState, source).ToList();
        if (ReAstralPartyModSettingsManager.GetEnableAllVariantPersonas(runState))
            return AddAllBuiltInVariantPersonas(runState, options);

        return AddChainedVariantPersonasIfNeeded(runState, options);
    }

    private static IReadOnlyList<RelicModel> AddAllBuiltInVariantPersonas(
        RunState runState,
        List<RelicModel> options)
    {
        var bannedRelicIds = ReAstralPartyModSettingsManager.GetBannedRelicIds(runState);
        var builtInVariants = PersonRelicRegistry.GetStartingBuiltInVariantPersonaRelics()
            .Where(relic => !BannedRelicRegistry.IsBanned(bannedRelicIds, relic))
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
        var bannedRelicIds = ReAstralPartyModSettingsManager.GetBannedRelicIds(runState);
        var allVariants = PersonRelicRegistry.GetStartingBuiltInVariantPersonaRelics()
            .Where(relic => !BannedRelicRegistry.IsBanned(bannedRelicIds, relic))
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
            $"Starting persona variant chain completed: totalOptions={options.Count} appended={appendedCount} variantCount={options.Count(PersonRelicRegistry.IsVariantPersonaRelic)}.");
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

    private static bool IsForcedCompatVariantRelic(RelicModel relic)
    {
        return (relic.CanonicalInstance ?? relic) is VariantPersonWindchaserThePlaneswalker or VariantPersonManosabaLinHiro;
    }

    internal static string GetRunKey(RunState runState)
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
