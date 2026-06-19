using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

// Legacy reference helper for the old treasure-room-style shared relic flow.
// The current starting persona selection no longer uses this as its main synchronization path.
internal static class TreasureRoomRelicSessionHelper
{
    private static readonly FieldInfo LocalPlayerIdField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_localPlayerId")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_localPlayerId");

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

    private static readonly FieldInfo VotesChangedField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "VotesChanged")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "VotesChanged");

    private static readonly MethodInfo EndRelicVotingMethod =
        AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "EndRelicVoting")
        ?? throw new MissingMethodException(typeof(TreasureRoomRelicSynchronizer).FullName, "EndRelicVoting");

    private static readonly MethodInfo BeginRelicPickingMethod =
        AccessTools.Method(typeof(TreasureRoomRelicSynchronizer), "BeginRelicPicking")
        ?? throw new MissingMethodException(typeof(TreasureRoomRelicSynchronizer).FullName, "BeginRelicPicking");

    public static bool TryBeginSession(
        TreasureRoomRelicSynchronizer synchronizer,
        IRunState runState,
        IReadOnlyList<RelicModel> relicOptions)
    {
        if (synchronizer.CurrentRelics != null)
            return false;

        CurrentRelicsField.SetValue(synchronizer, relicOptions.ToList());

        var votes = GetVotes(synchronizer);
        votes.Clear();
        ResetSessionState(synchronizer);

        foreach (var player in runState.Players)
            votes.Add(CreateInitialVote(runState, player, relicOptions.Count));

        BeginRelicPickingMethod.Invoke(synchronizer, null);

        if (relicOptions.Count == 0)
            EndRelicVotingMethod.Invoke(synchronizer, null);
        else
            InvokeVotesChanged(synchronizer);

        return true;
    }

    public static void SubmitLocalPick(TreasureRoomRelicSynchronizer synchronizer, IRunState runState, int index)
    {
        var localPlayer = LocalContext.GetMe(runState.Players);
        if (localPlayer == null)
            throw new InvalidOperationException("Local player was not found for starting persona selection.");

        synchronizer.OnPicked(localPlayer, index);
    }

    public static void EndSessionSafely(TreasureRoomRelicSynchronizer synchronizer)
    {
        if (synchronizer.CurrentRelics == null)
            return;

        EndRelicVotingMethod.Invoke(synchronizer, null);
        InvokeVotesChanged(synchronizer);
    }

    public static List<TreasureRoomRelicSynchronizer.PlayerVote> GetVotes(TreasureRoomRelicSynchronizer synchronizer)
    {
        return (List<TreasureRoomRelicSynchronizer.PlayerVote>)VotesField.GetValue(synchronizer)!;
    }

    public static void ResetSessionState(TreasureRoomRelicSynchronizer synchronizer)
    {
        PredictedVoteField.SetValue(synchronizer, null);
        SinglePlayerSkippedField.SetValue(synchronizer, false);
    }

    public static void PrefillFakeMultiplayerVotes(
        TreasureRoomRelicSynchronizer synchronizer,
        IPlayerCollection playerCollection,
        Rng rng,
        List<TreasureRoomRelicSynchronizer.PlayerVote> votes,
        int relicCount)
    {
        if (!RunManager.Instance.IsSingleplayerOrFakeMultiplayer || playerCollection.Players.Count <= 1 ||
            relicCount <= 0)
            return;

        var localPlayerId = (ulong)LocalPlayerIdField.GetValue(synchronizer)!;
        var localPlayer = playerCollection.GetPlayer(localPlayerId);
        foreach (var player in playerCollection.Players)
        {
            if (player == localPlayer)
                continue;

            var vote = votes[playerCollection.GetPlayerSlotIndex(player)];
            vote.index = rng.NextInt(relicCount);
            vote.voteReceived = true;
        }
    }

    public static void InvokeVotesChanged(TreasureRoomRelicSynchronizer synchronizer)
    {
        ((Action?)VotesChangedField.GetValue(synchronizer))?.Invoke();
    }

    private static TreasureRoomRelicSynchronizer.PlayerVote CreateInitialVote(
        IRunState runState,
        Player player,
        int optionCount)
    {
        if (RunManager.Instance.IsSingleplayerOrFakeMultiplayer
            && runState.Players.Count > 1
            && player.NetId != RunManager.Instance.NetService.NetId
            && optionCount > 0)
            return new TreasureRoomRelicSynchronizer.PlayerVote
            {
                index = runState.Rng.TreasureRoomRelics.NextInt(optionCount),
                voteReceived = true
            };

        return new TreasureRoomRelicSynchronizer.PlayerVote
        {
            voteReceived = false
        };
    }
}
