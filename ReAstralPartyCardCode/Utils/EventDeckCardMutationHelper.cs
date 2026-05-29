using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EventDeckCardMutationHelper
{
    private const int MaxSynchronizerWaitFrames = 60;

    private enum DeckMutationKind
    {
        Upgrade = 1,
        Downgrade = 2,
        Remove = 3
    }

    public static async Task Upgrade(Player owner, IReadOnlyList<CardModel> selectedCards, string context)
    {
        var cardsToMutate = await SynchronizeDeckCards(owner, selectedCards, DeckMutationKind.Upgrade, context);
        foreach (var card in cardsToMutate)
            CardCmd.Upgrade(card);
    }

    public static async Task Downgrade(Player owner, IReadOnlyList<CardModel> selectedCards, string context)
    {
        var cardsToMutate = await SynchronizeDeckCards(owner, selectedCards, DeckMutationKind.Downgrade, context);
        foreach (var card in cardsToMutate)
            card.DowngradeInternal();
    }

    public static async Task Remove(Player owner, IReadOnlyList<CardModel> selectedCards, string context)
    {
        var cardsToMutate = await SynchronizeDeckCards(owner, selectedCards, DeckMutationKind.Remove, context);
        foreach (var card in cardsToMutate)
            EventDeckCardHelper.RemoveCardFromRunDeck(owner, card);
    }

    private static async Task<IReadOnlyList<CardModel>> SynchronizeDeckCards(
        Player owner,
        IReadOnlyList<CardModel> selectedCards,
        DeckMutationKind mutationKind,
        string context)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(selectedCards);

        var fallbackCards = selectedCards
            .Where(static card => card != null)
            .ToList();
        if (fallbackCards.Count == 0)
            return [];

        var selectedIndexes = GetStableDeckIndexes(owner, fallbackCards);
        if (selectedIndexes.Count != fallbackCards.Count)
        {
            Log.Warn(
                $"[{MainFile.ModId}] Deck mutation index resolution mismatch before sync | owner={owner.NetId} | context={context} | kind={mutationKind} | selected={fallbackCards.Count} | resolved={selectedIndexes.Count}");
        }

        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        if (runManager == null || netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return ResolveDeckCardsByIndexes(owner, selectedIndexes, fallbackCards);

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            Log.Warn(
                $"[{MainFile.ModId}] Deck mutation sync fallback due to missing synchronizer | owner={owner.NetId} | context={context} | kind={mutationKind}");
            return ResolveDeckCardsByIndexes(owner, selectedIndexes, fallbackCards);
        }

        var sessionKey = $"deck_mutation|{context}|{owner.NetId}|{(int)mutationKind}";
        var choiceId = synchronizer.ReserveChoiceId(owner);
        if (IsLocalPlayer(runManager, owner))
        {
            synchronizer.SyncLocalChoice(
                owner,
                choiceId,
                AstralChoiceProtocol.CreateIndexedEnvelope(
                    AstralChoiceKind.DeckCardMutationSelection,
                    owner.RunState as RunState,
                    sessionKey,
                    0,
                    selectedIndexes));
            Log.Info(
                $"[{MainFile.ModId}] Synced deck mutation targets | owner={owner.NetId} | context={context} | kind={mutationKind} | choiceId={choiceId} | count={selectedIndexes.Count}");
            return ResolveDeckCardsByIndexes(owner, selectedIndexes, fallbackCards);
        }

        var remoteChoice = await DeterministicMultiplayerChoiceHelper.WaitForRemoteIndexedEnvelope(
            synchronizer,
            owner,
            choiceId,
            AstralChoiceKind.DeckCardMutationSelection,
            owner.RunState as RunState,
            sessionKey,
            context);
        if (remoteChoice == null)
        {
            Log.Warn(
                $"[{MainFile.ModId}] Deck mutation sync fallback due to missing remote payload | owner={owner.NetId} | context={context} | kind={mutationKind} | choiceId={choiceId}");
            return ResolveDeckCardsByIndexes(owner, selectedIndexes, fallbackCards);
        }

        Log.Info(
            $"[{MainFile.ModId}] Received deck mutation targets | owner={owner.NetId} | context={context} | kind={mutationKind} | choiceId={choiceId} | count={remoteChoice.Value.Payload.Count}");
        return ResolveDeckCardsByIndexes(owner, remoteChoice.Value.Payload, fallbackCards);
    }

    private static List<int> GetStableDeckIndexes(Player owner, IReadOnlyList<CardModel> selectedCards)
    {
        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        return selectedCards
            .Select(card => deckCards.IndexOf(card))
            .Where(static index => index >= 0)
            .Distinct()
            .OrderBy(static index => index)
            .ToList();
    }

    private static IReadOnlyList<CardModel> ResolveDeckCardsByIndexes(
        Player owner,
        IReadOnlyList<int> indexes,
        IReadOnlyList<CardModel> fallbackCards)
    {
        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var resolvedCards = indexes
            .Where(index => index >= 0 && index < deckCards.Count)
            .Select(index => deckCards[index])
            .ToList();
        if (resolvedCards.Count == indexes.Count && resolvedCards.Count > 0)
            return resolvedCards;

        Log.Warn(
            $"[{MainFile.ModId}] Deck mutation index replay mismatch; using fallback cards | owner={owner.NetId} | requested={indexes.Count} | resolved={resolvedCards.Count}");
        return fallbackCards;
    }

    private static bool IsLocalPlayer(RunManager runManager, Player player)
    {
        return player.NetId != 0UL && player.NetId == runManager.NetService.NetId;
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
}
