using System.Reflection;
using AstralPartyMod.AstralPartyCardCode.Modifiers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
public static class StarEngineTreasureRoomRelicPatch
{
    private static readonly FieldInfo PlayerCollectionField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_playerCollection")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_playerCollection");

    private static readonly FieldInfo LocalPlayerIdField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_localPlayerId")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_localPlayerId");

    private static readonly FieldInfo RngField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_rng")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_rng");

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

    [HarmonyPrefix]
    public static bool Prefix(TreasureRoomRelicSynchronizer __instance)
    {
        var playerCollection = (IPlayerCollection)PlayerCollectionField.GetValue(__instance)!;
        if (!playerCollection.Players.Any(player => StarEngineModifier.IsActive(player.RunState)))
            return true;

        if (__instance.CurrentRelics != null)
            throw new InvalidOperationException(
                "Attempted to start new relic picking session while one was already occurring!");

        var rng = (Rng)RngField.GetValue(__instance)!;
        var currentRelics = new List<RelicModel>();
        var votes = (List<TreasureRoomRelicSynchronizer.PlayerVote>)VotesField.GetValue(__instance)!;

        CurrentRelicsField.SetValue(__instance, currentRelics);
        votes.Clear();
        PredictedVoteField.SetValue(__instance, null);
        SinglePlayerSkippedField.SetValue(__instance, false);

        foreach (var player in playerCollection.Players)
        {
            votes.Add(new TreasureRoomRelicSynchronizer.PlayerVote { voteReceived = false });

            var runState = player.RunState;
            if (!Hook.ShouldGenerateTreasure(runState, player))
                continue;

            var rarity = RelicFactory.RollRarity(rng);
            currentRelics.Add(
                TokenRelicRegistry.GetRandomTokenRelicForTreasure(rarity, rng)
                ?? RelicFactory.FallbackRelic);
        }

        if (currentRelics.Count == 0)
        {
            EndRelicVotingMethod.Invoke(__instance, null);
            return false;
        }

        PrefillFakeMultiplayerVotes(__instance, playerCollection, rng, votes, currentRelics.Count);
        ((Action?)VotesChangedField.GetValue(__instance))?.Invoke();
        return false;
    }

    private static void PrefillFakeMultiplayerVotes(
        TreasureRoomRelicSynchronizer synchronizer,
        IPlayerCollection playerCollection,
        Rng rng,
        List<TreasureRoomRelicSynchronizer.PlayerVote> votes,
        int relicCount)
    {
        if (!RunManager.Instance.IsSinglePlayerOrFakeMultiplayer || playerCollection.Players.Count <= 1)
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
}
