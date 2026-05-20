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
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class StartingPersonaRelicSelectionPatch : IPatchMethod
{
    public static string PatchId => "starting_persona_relic_selection_patch";
    public static bool IsCritical => false;
    public static string Description => "Open the starting persona relic selection after a run starts";

    private static readonly object SelectionLifecycleLock = new();
    private static readonly HashSet<string> ActiveRunKeys = [];
    private static readonly HashSet<string> CompletedRunKeys = [];

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))];
    }

    public static void Postfix(RunManager __instance, ref Task __result)
    {
        __result = RunAfterFinalizeStartingRelics(__result, __instance);
    }

    private static async Task RunAfterFinalizeStartingRelics(Task originalTask, RunManager runManager)
    {
        await originalTask;
        var runState = runManager.DebugOnlyGetState();
        if (runState == null)
        {
            MainFile.Logger.Warn(
                "Starting persona relic selection skipped because run state was unavailable after starting relic finalization.");
            return;
        }

        MainFile.Logger.Info(
            $"Starting persona relic selection patch entered: seed={runState.Rng.StringSeed} players={runState.Players.Count}.");
        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.StartRunBootstrap, "starting persona selection bootstrap"))
        {
            MainFile.Logger.Warn(
                "Starting persona relic selection bootstrap was not ready yet; waiting briefly instead of skipping.");
            await WaitForBootstrapReadinessAsync();
        }

        MainFile.Logger.Info("Starting persona relic selection patch resumed after starting relic finalization.");
        await ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);
        var gameType = RunManager.Instance.NetService.Type;
        MainFile.Logger.Info(
            $"Starting persona relic selection run gate: netMode={gameType} players={runState.Players.Count}.");
        if (!ShouldOpenStartingPersonaRelicSelection(runState, out var skipReason))
        {
            MainFile.Logger.Info($"Starting persona relic selection skipped: {skipReason}.");
            return;
        }

        var runKey = GetRunKey(runState);
        if (!TryBeginSelection(runKey))
        {
            MainFile.Logger.Info(
                $"Starting persona relic selection skipped because run '{runKey}' is already processing.");
            return;
        }

        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
        {
            EndSelection(runKey, false);
            MainFile.Logger.Warn("Starting persona relic selection skipped because overlay stack is not ready.");
            AstralNotificationService.ShowWarning(
                AstralNotificationModule.Multiplayer,
                "开局人格选择界面未能正常打开。",
                "联机提示");
            return;
        }

        var relicOptions = CreateStartingPersonaRelicOptions(runState);
        if (relicOptions.Count == 0)
        {
            EndSelection(runKey, false);
            MainFile.Logger.Warn("Starting persona relic selection skipped because no persona relics are registered.");
            AstralNotificationService.ShowWarning(
                AstralNotificationModule.Multiplayer,
                "未找到可用的人格选项，请反馈日志。",
                "联机提示");
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
                AstralNotificationService.ShowInfo(
                    AstralNotificationModule.Multiplayer,
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

        MainFile.Logger.Info(
            $"Starting persona relic selection options prepared: count={relicOptions.Count} runKey={runKey}.");
        var screen = StartingPersonaRelicSelectionScreen.Create(runState, relicOptions);
        try
        {
            overlayStack.Push(screen);
            MainFile.Logger.Info(
                $"Starting persona relic selection shown with {relicOptions.Count} persona relic options.");
            await screen.RelicPickingFinished();
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

    private static async Task WaitForBootstrapReadinessAsync()
    {
        for (var attempt = 0; attempt < 300; attempt++)
        {
            if (RunManager.Instance?.DebugOnlyGetState() != null)
                return;

            var overlayStack = NOverlayStack.Instance;
            if (NGame.Instance?.IsInsideTree() == true
                && overlayStack != null
                && Godot.GodotObject.IsInstanceValid(overlayStack)
                && overlayStack.IsInsideTree())
                return;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), Godot.SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var attempt = 0; attempt < 600; attempt++)
        {
            var overlayStack = NOverlayStack.Instance;
            if (overlayStack != null
                && Godot.GodotObject.IsInstanceValid(overlayStack)
                && overlayStack.IsInsideTree())
                return overlayStack;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), Godot.SceneTree.SignalName.ProcessFrame);
            else
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

            return allAvailableOptions.Count > 0 ? allAvailableOptions : allPersonaRelics;
        }

        var targetCount = runState.Players.Count * 2 + 2;
        var options = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                allPersonaRelics
                    .Where(relic => !ownedPersonaRelicIds.Contains(relic.Id))
                    .ToList(),
                relic => relic.Id.Entry,
                MainFile.ModId,
                "starting_persona_primary_pool",
                runState.Rng.StringSeed,
                runState.Players.Count)
            .Take(targetCount)
            .ToList();

        if (options.Count >= targetCount)
            return options;

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
        return options;
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
}
