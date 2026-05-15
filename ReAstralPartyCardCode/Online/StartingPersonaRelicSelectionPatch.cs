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
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NGame), "StartRun")]
public static class StartingPersonaRelicSelectionPatch
{
    private static readonly object SelectionLifecycleLock = new();
    private static readonly HashSet<string> ActiveRunKeys = [];
    private static readonly HashSet<string> CompletedRunKeys = [];

    [HarmonyPostfix]
    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterStartRun(__result, runState);
    }

    private static async Task RunAfterStartRun(Task originalTask, RunState runState)
    {
        await originalTask;
        var gameType = RunManager.Instance.NetService.Type;
        MainFile.Logger.Info($"Starting persona relic selection run gate: netMode={gameType} players={runState.Players.Count}.");
        if (!ShouldOpenStartingPersonaRelicSelection(runState, out var skipReason))
        {
            MainFile.Logger.Info($"Starting persona relic selection skipped: {skipReason}.");
            return;
        }

        var runKey = GetRunKey(runState);
        if (!TryBeginSelection(runKey))
        {
            MainFile.Logger.Info($"Starting persona relic selection skipped because run '{runKey}' is already processing.");
            return;
        }

        var overlayStack = NOverlayStack.Instance;
        if (overlayStack == null)
        {
            EndSelection(runKey, completed: false);
            MainFile.Logger.Warn("Starting persona relic selection skipped because overlay stack is not ready.");
            return;
        }

        var relicOptions = CreateStartingPersonaRelicOptions(runState);
        if (relicOptions.Count == 0)
        {
            EndSelection(runKey, completed: false);
            MainFile.Logger.Warn("Starting persona relic selection skipped because no persona relics are registered.");
            return;
        }

        var screen = StartingPersonaRelicSelectionScreen.Create(runState, relicOptions);
        try
        {
            overlayStack.Push(screen);
            MainFile.Logger.Info(
                $"Starting persona relic selection shown with {relicOptions.Count} persona relic options.");
            await screen.RelicPickingFinished();
            EndSelection(runKey, completed: true);
        }
        catch
        {
            EndSelection(runKey, completed: false);
            throw;
        }
        finally
        {
            screen.Close();
        }
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
        var targetCount = runState.Players.Count * 2 + 2;
        var allPersonaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics()
            .OrderBy(relic => relic.Id.Entry)
            .ToList();

        var ownedPersonaRelicIds = runState.Players
            .SelectMany(player => player.Relics)
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

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
