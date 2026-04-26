using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NGame), "StartRun")]
public static class StartingPersonaRelicSelectionPatch
{
    private static readonly FieldInfo CurrentRelicsField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_currentRelics")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_currentRelics");

    private static readonly FieldInfo VotesField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_votes")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_votes");

    private static readonly FieldInfo PredictedVoteField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_predictedVote")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_predictedVote");

    private static readonly FieldInfo SinglePlayerSkippedField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_singlePlayerSkipped")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_singlePlayerSkipped");

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

        if (!TryBeginPersonaRelicPicking(runState, relicOptions))
            return;

        var screen = StartingPersonaRelicSelectionScreen.Create(runState, relicOptions);
        overlayStack.Push(screen);

        MainFile.Logger.Info(
            $"Starting persona relic shared selection shown with {relicOptions.Count} persona relic options.");

        await screen.RelicPickingFinished();
        screen.Close();
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

    private static bool TryBeginPersonaRelicPicking(RunState runState, IReadOnlyList<RelicModel> relicOptions)
    {
        var synchronizer = RunManager.Instance.TreasureRoomRelicSynchronizer;
        if (synchronizer.CurrentRelics != null)
        {
            MainFile.Logger.Warn("Starting persona relic selection skipped because a relic picking session is already active.");
            return false;
        }

        CurrentRelicsField.SetValue(synchronizer, relicOptions.ToList());
        PredictedVoteField.SetValue(synchronizer, null);
        SinglePlayerSkippedField.SetValue(synchronizer, false);

        var votes = (List<TreasureRoomRelicSynchronizer.PlayerVote>)VotesField.GetValue(synchronizer)!;
        votes.Clear();
        foreach (var player in runState.Players)
        {
            votes.Add(CreateInitialVote(runState, player, relicOptions.Count));
        }

        return true;
    }

    private static TreasureRoomRelicSynchronizer.PlayerVote CreateInitialVote(RunState runState, Player player, int optionCount)
    {
        if (RunManager.Instance.IsSinglePlayerOrFakeMultiplayer
            && runState.Players.Count > 1
            && player.NetId != RunManager.Instance.NetService.NetId
            && optionCount > 0)
        {
            return new TreasureRoomRelicSynchronizer.PlayerVote
            {
                index = runState.Rng.TreasureRoomRelics.NextInt(optionCount),
                voteReceived = true
            };
        }

        return new TreasureRoomRelicSynchronizer.PlayerVote
        {
            voteReceived = false
        };
    }
}
