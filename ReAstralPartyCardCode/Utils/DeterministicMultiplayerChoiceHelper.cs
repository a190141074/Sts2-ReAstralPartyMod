using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class DeterministicMultiplayerChoiceHelper
{
    private const int MaxSynchronizerWaitFrames = 60;
    private const int ChoiceMagic = unchecked((int)0x5241504D);
    private const int ChoiceKindRelicSelection = 1;
    private const int ChoiceKindCanonicalCardSelection = 2;

    public static async Task<RelicModel?> SelectRelicForPlayer(
        Player player,
        IReadOnlyList<RelicModel> options,
        string context)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(options);

        var runManager = RunManager.Instance;
        var gameType = runManager.NetService.Type;
        if (gameType is NetGameType.Singleplayer or NetGameType.None)
            return await ShowLocalRelicSelection(player, options);

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
            return await ShowLocalRelicSelection(player, options);

        var choiceId = synchronizer.ReserveChoiceId(player);
        if (IsLocalPlayer(runManager, player))
        {
            var selectedRelic = await ShowLocalRelicSelection(player, options);
            var selectedIndex = selectedRelic == null ? -1 : IndexOfRelic(options, selectedRelic);
            synchronizer.SyncLocalChoice(player, choiceId, CreateRelicChoiceResult(selectedIndex));
            Log.Info($"[{MainFile.ModId}] Synced local relic choice: context={context} player={player.NetId} choiceId={choiceId} index={selectedIndex}");
            return selectedRelic;
        }

        var remoteChoice = await WaitForRemoteChoice(synchronizer, player, choiceId, context);
        var remoteIndex = DecodeRelicChoiceIndex(remoteChoice);
        Log.Info($"[{MainFile.ModId}] Received remote relic choice: context={context} player={player.NetId} choiceId={choiceId} index={remoteIndex}");
        return remoteIndex >= 0 && remoteIndex < options.Count ? options[remoteIndex] : null;
    }

    public static async Task<CardModel?> SelectCanonicalCardForPlayer(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyList<CardModel> options,
        bool canSkip,
        string context)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Count == 0)
            return null;

        var runManager = RunManager.Instance;
        var gameType = runManager.NetService.Type;
        if (gameType is NetGameType.Singleplayer or NetGameType.None)
            return await ShowLocalCanonicalCardSelection(player, options, canSkip);

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
            return await ShowLocalCanonicalCardSelection(player, options, canSkip);

        var choiceId = synchronizer.ReserveChoiceId(player);
        await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
        try
        {
            if (IsLocalPlayer(runManager, player))
            {
                var selectedCard = await ShowLocalCanonicalCardSelection(player, options, canSkip);
                var canonicalChoice = selectedCard?.CanonicalInstance ?? selectedCard;
                synchronizer.SyncLocalChoice(player, choiceId, PlayerChoiceResult.FromCanonicalCard(canonicalChoice));
                Log.Info(
                    $"[{MainFile.ModId}] Synced local card choice: context={context} player={player.NetId} choiceId={choiceId} card={canonicalChoice?.Id.Entry ?? "<null>"}");
                return canonicalChoice;
            }

            var remoteChoice = await synchronizer.WaitForRemoteChoice(player, choiceId);
            var remoteCard = remoteChoice.AsCanonicalCard();
            Log.Info(
                $"[{MainFile.ModId}] Received remote card choice: context={context} player={player.NetId} choiceId={choiceId} card={remoteCard?.Id.Entry ?? "<null>"}");
            return remoteCard;
        }
        finally
        {
            await choiceContext.SignalPlayerChoiceEnded();
        }
    }

    public static IReadOnlyList<T> OrderDeterministically<T>(
        IEnumerable<T> source,
        Func<T, string> idSelector,
        params object?[] contextParts)
    {
        return source
            .OrderBy(item => ComputeDeterministicScore(idSelector(item), contextParts))
            .ThenBy(idSelector, StringComparer.Ordinal)
            .ToList();
    }

    public static T? PickDeterministically<T>(
        IReadOnlyList<T> candidates,
        Func<T, string> idSelector,
        params object?[] contextParts)
    {
        if (candidates.Count == 0)
            return default;

        return OrderDeterministically(candidates, idSelector, contextParts)[0];
    }

    public static int RollDeterministically(
        int minInclusive,
        int maxExclusive,
        params object?[] contextParts)
    {
        if (maxExclusive <= minInclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));

        var range = maxExclusive - minInclusive;
        var key = string.Join("|", contextParts.Select(static part => part?.ToString() ?? "<null>"));
        var score = unchecked((uint)StringHelper.GetDeterministicHashCode(key));
        return minInclusive + (int)(score % (uint)range);
    }

    private static async Task<PlayerChoiceSynchronizer?> WaitForPlayerChoiceSynchronizerAsync(RunManager runManager)
    {
        for (var i = 0; i < MaxSynchronizerWaitFrames; i++)
        {
            if (runManager.PlayerChoiceSynchronizer != null)
                return runManager.PlayerChoiceSynchronizer;

            await Task.Yield();
        }

        return runManager.PlayerChoiceSynchronizer;
    }

    private static async Task<PlayerChoiceResult> WaitForRemoteChoice(
        PlayerChoiceSynchronizer synchronizer,
        Player player,
        uint initialChoiceId,
        string context)
    {
        var choiceId = initialChoiceId;
        while (true)
        {
            var remoteChoice = await synchronizer.WaitForRemoteChoice(player, choiceId);
            if (TryDecodeRelicChoiceIndex(remoteChoice, out _))
                return remoteChoice;

            Log.Warn($"[{MainFile.ModId}] Skipped non-relic multiplayer choice: context={context} player={player.NetId} choiceId={choiceId} result={remoteChoice}");
            choiceId = synchronizer.ReserveChoiceId(player);
        }
    }

    private static PlayerChoiceResult CreateRelicChoiceResult(int selectedIndex)
    {
        return PlayerChoiceResult.FromIndexes([ChoiceMagic, ChoiceKindRelicSelection, selectedIndex]);
    }

    private static int DecodeRelicChoiceIndex(PlayerChoiceResult result)
    {
        return TryDecodeRelicChoiceIndex(result, out var selectedIndex) ? selectedIndex : -1;
    }

    private static bool TryDecodeRelicChoiceIndex(PlayerChoiceResult result, out int selectedIndex)
    {
        selectedIndex = -1;
        try
        {
            var payload = result.AsIndexes();
            if (payload == null || payload.Count < 3)
                return false;
            if (payload[0] != ChoiceMagic || payload[1] != ChoiceKindRelicSelection)
                return false;

            selectedIndex = payload[2];
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool IsLocalPlayer(RunManager runManager, Player player)
    {
        return player.NetId != 0UL && player.NetId == runManager.NetService.NetId;
    }

    private static async Task<RelicModel?> ShowLocalRelicSelection(Player player, IReadOnlyList<RelicModel> options)
    {
        var screen = NChooseARelicSelection.ShowScreen(options);
        if (LocalContext.IsMe(player))
        {
            foreach (var relic in options)
                SaveManager.Instance.MarkRelicAsSeen(relic);
        }

        return (await screen.RelicsSelected()).FirstOrDefault();
    }

    private static async Task<CardModel?> ShowLocalCanonicalCardSelection(
        Player player,
        IReadOnlyList<CardModel> options,
        bool canSkip)
    {
        NPlayerHand.Instance?.CancelAllCardPlay();
        var screen = NChooseACardSelectionScreen.ShowScreen(options, canSkip);
        if (LocalContext.IsMe(player))
        {
            foreach (var card in options)
                SaveManager.Instance.MarkCardAsSeen(card);
        }

        var selectedCard = (await screen.CardsSelected()).FirstOrDefault();
        return selectedCard?.CanonicalInstance ?? selectedCard;
    }

    private static int IndexOfRelic(IReadOnlyList<RelicModel> relics, RelicModel relic)
    {
        for (var i = 0; i < relics.Count; i++)
        {
            if (ReferenceEquals(relics[i], relic))
                return i;

            var left = relics[i].CanonicalInstance?.Id ?? relics[i].Id;
            var right = relic.CanonicalInstance?.Id ?? relic.Id;
            if (left == right)
                return i;
        }

        return -1;
    }

    private static uint ComputeDeterministicScore(string itemId, IReadOnlyList<object?> contextParts)
    {
        var parts = contextParts
            .Select(static part => part?.ToString() ?? "<null>")
            .Append(itemId);
        var key = string.Join("|", parts);
        return unchecked((uint)StringHelper.GetDeterministicHashCode(key));
    }
}
