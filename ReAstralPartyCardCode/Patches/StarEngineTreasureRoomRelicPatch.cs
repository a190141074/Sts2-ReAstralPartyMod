using System.Reflection;
using ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(TreasureRoomRelicSynchronizer), nameof(TreasureRoomRelicSynchronizer.BeginRelicPicking))]
public static class StarEngineTreasureRoomRelicPatch
{
    private static readonly FieldInfo PlayerCollectionField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_playerCollection")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_playerCollection");

    private static readonly FieldInfo RngField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_rng")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_rng");

    private static readonly FieldInfo CurrentRelicsField =
        AccessTools.Field(typeof(TreasureRoomRelicSynchronizer), "_currentRelics")
        ?? throw new MissingFieldException(typeof(TreasureRoomRelicSynchronizer).FullName, "_currentRelics");

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
        var votes = TreasureRoomRelicSessionHelper.GetVotes(__instance);

        CurrentRelicsField.SetValue(__instance, currentRelics);
        votes.Clear();
        TreasureRoomRelicSessionHelper.ResetSessionState(__instance);

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
            TreasureRoomRelicSessionHelper.EndSessionSafely(__instance);
            return false;
        }

        TreasureRoomRelicSessionHelper.PrefillFakeMultiplayerVotes(__instance, playerCollection, rng, votes,
            currentRelics.Count);
        TreasureRoomRelicSessionHelper.InvokeVotesChanged(__instance);
        return false;
    }
}
