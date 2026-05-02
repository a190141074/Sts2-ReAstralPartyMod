using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NGame), "StartRun")]
public static class StartingPersonaRelicSelectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterStartRun(__result, runState);
    }

    private static async Task RunAfterStartRun(Task originalTask, RunState runState)
    {
        await originalTask;

        var overlayStack = NOverlayStack.Instance;
        if (overlayStack == null)
        {
            MainFile.Logger.Warn("Starting persona relic selection skipped because overlay stack is not ready.");
            return;
        }

        var relicOptions = CreateStartingPersonaRelicOptions(runState);
        if (relicOptions.Count == 0)
        {
            MainFile.Logger.Warn("Starting persona relic selection skipped because no persona relics are registered.");
            return;
        }

        var synchronizer = RunManager.Instance.TreasureRoomRelicSynchronizer;
        if (!TreasureRoomRelicSessionHelper.TryBeginSession(synchronizer, runState, relicOptions))
        {
            MainFile.Logger.Warn(
                "Starting persona relic selection skipped because a relic picking session is already active.");
            return;
        }

        var screen = StartingPersonaRelicSelectionScreen.Create(runState, relicOptions);
        try
        {
            overlayStack.Push(screen);
            MainFile.Logger.Info(
                $"Starting persona relic shared selection shown with {relicOptions.Count} persona relic options.");
            await screen.RelicPickingFinished();
        }
        finally
        {
            screen.Close();
            TreasureRoomRelicSessionHelper.EndSessionSafely(synchronizer);
        }
    }

    private static IReadOnlyList<RelicModel> CreateStartingPersonaRelicOptions(RunState runState)
    {
        var targetCount = runState.Players.Count + 3;
        var allPersonaRelics = PersonaRelicRegistry.GetCanonicalPersonaRelics()
            .OrderBy(relic => relic.Id.Entry)
            .ToList();

        var ownedPersonaRelicIds = runState.Players
            .SelectMany(player => player.Relics)
            .Select(relic => relic.CanonicalInstance.Id)
            .ToHashSet();

        var options = allPersonaRelics
            .Where(relic => !ownedPersonaRelicIds.Contains(relic.Id))
            .ToList()
            .UnstableShuffle(runState.Rng.TreasureRoomRelics)
            .Take(targetCount)
            .ToList();

        if (options.Count >= targetCount)
            return options;

        var selectedIds = options.Select(relic => relic.Id).ToHashSet();
        var fallbackOptions = allPersonaRelics
            .Where(relic => !selectedIds.Contains(relic.Id))
            .ToList()
            .UnstableShuffle(runState.Rng.TreasureRoomRelics);

        options.AddRange(fallbackOptions.Take(targetCount - options.Count));
        return options;
    }
}
