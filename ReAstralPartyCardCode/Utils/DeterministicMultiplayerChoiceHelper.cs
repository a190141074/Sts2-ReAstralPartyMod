using MegaCrit.Sts2.Core.Commands;
using Godot;
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
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class DeterministicMultiplayerChoiceHelper
{
    private const int MaxSynchronizerWaitFrames = 60;
    private const int MaxForeignChoiceMessagesPerChoice = 16;

    internal readonly record struct RemoteIndexedChoiceEnvelope(
        PlayerChoiceResult RawResult,
        AstralChoiceKind Kind,
        int Sequence,
        IReadOnlyList<int> Payload);

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

        var sessionKey = BuildSessionKey(AstralChoiceKind.RelicSelection, player, context);
        var choiceId = synchronizer.ReserveChoiceId(player);
        if (IsLocalPlayer(runManager, player))
        {
            var selectedRelic = await ShowLocalRelicSelection(player, options);
            var selectedIndex = selectedRelic == null ? -1 : IndexOfRelic(options, selectedRelic);
                synchronizer.SyncLocalChoice(
                    player,
                    choiceId,
                    AstralChoiceProtocol.CreateIndexedEnvelope(
                        AstralChoiceKind.RelicSelection,
                        (RunState?)player.RunState,
                        sessionKey,
                        0,
                        [selectedIndex]));
            AstralTelemetry.RecordTokenChoice(player, context, options, selectedRelic, 0);
            Log.Info(
                $"[{MainFile.ModId}] Synced local relic choice: context={context} player={player.NetId} choiceId={choiceId} index={selectedIndex}");
            return selectedRelic;
        }

        var remoteChoice = await WaitForRemoteIndexedEnvelope(
            synchronizer,
            player,
            choiceId,
            AstralChoiceKind.RelicSelection,
            (RunState?)player.RunState,
            sessionKey,
            context);
        var remotePayload = remoteChoice?.Payload ?? [];

        var remoteIndex = remotePayload.Count > 0 ? remotePayload[0] : -1;
        var remoteRelic = remoteIndex >= 0 && remoteIndex < options.Count ? options[remoteIndex] : null;
        AstralTelemetry.RecordTokenChoice(player, context, options, remoteRelic, 0);
        Log.Info(
            $"[{MainFile.ModId}] Received remote relic choice: context={context} player={player.NetId} choiceId={choiceId} index={remoteIndex}");
        return remoteRelic;
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

        var sessionKey = BuildSessionKey(AstralChoiceKind.CanonicalCardSelection, player, context);
        var choiceId = synchronizer.ReserveChoiceId(player);
        await choiceContext.SignalPlayerChoiceBegun(PlayerChoiceOptions.None);
        try
        {
            if (IsLocalPlayer(runManager, player))
            {
                var selectedCard = await ShowLocalCanonicalCardSelection(player, options, canSkip);
                var canonicalChoice = selectedCard?.CanonicalInstance ?? selectedCard;
                var selectedIndex = canonicalChoice == null ? -1 : IndexOfCard(options, canonicalChoice);
                synchronizer.SyncLocalChoice(
                    player,
                    choiceId,
                    AstralChoiceProtocol.CreateIndexedEnvelope(
                        AstralChoiceKind.CanonicalCardSelection,
                        (RunState?)player.RunState,
                        sessionKey,
                        0,
                        [selectedIndex]));
                Log.Info(
                    $"[{MainFile.ModId}] Synced local card choice: context={context} player={player.NetId} choiceId={choiceId} card={canonicalChoice?.Id.Entry ?? "<null>"}");
                return canonicalChoice;
            }

            var remoteChoice = await WaitForRemoteIndexedEnvelope(
                synchronizer,
                player,
                choiceId,
                AstralChoiceKind.CanonicalCardSelection,
                (RunState?)player.RunState,
                sessionKey,
                context);
            var remotePayload = remoteChoice?.Payload ?? [];

            var remoteIndex = remotePayload.Count > 0 ? remotePayload[0] : -1;
            var remoteCard = remoteIndex >= 0 && remoteIndex < options.Count
                ? options[remoteIndex].CanonicalInstance ?? options[remoteIndex]
                : null;
            Log.Info(
                $"[{MainFile.ModId}] Received remote card choice: context={context} player={player.NetId} choiceId={choiceId} card={remoteCard?.Id.Entry ?? "<null>"}");
            return remoteCard;
        }
        finally
        {
            await choiceContext.SignalPlayerChoiceEnded();
        }
    }

    public static async Task<IReadOnlyList<CardModel>> SelectHandCardsForPlayer(
        PlayerChoiceContext choiceContext,
        Player player,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? predicate = null,
        AbstractModel? selectionSource = null)
    {
        ArgumentNullException.ThrowIfNull(choiceContext);
        ArgumentNullException.ThrowIfNull(player);

        if (player.Creature?.CombatState == null)
            return [];

        var selectedCards = selectionSource == null
            ? await CardSelectCmd.FromHand(
                choiceContext,
                player,
                prefs,
                predicate,
                null!)
            : await CardSelectCmd.FromHand(
                choiceContext,
                player,
                prefs,
                predicate,
                selectionSource);

        return selectedCards.ToList();
    }

    public static async Task<RefreshableTokenRelicSelectionResult> SelectRefreshableRelicForPlayer(
        Player player,
        IReadOnlyList<RelicModel> options,
        int rerollCount,
        string title,
        string subtitlePrefix,
        string probabilityText,
        string context,
        Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> rerollFunc,
        Func<IReadOnlyList<int>, IReadOnlyList<RelicModel>> rebuildFromHistory)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(options);

        var runManager = RunManager.Instance;
        var gameType = runManager.NetService.Type;
        if (gameType is NetGameType.Singleplayer or NetGameType.None)
        {
            var localResult = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title,
                subtitlePrefix, probabilityText, rerollFunc);
            AstralTelemetry.RecordTokenChoice(player, context, localResult.FinalOptions, localResult.SelectedRelic,
                localResult.RerollHistory.Count);
            return localResult;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            var localResult = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title,
                subtitlePrefix, probabilityText, rerollFunc);
            AstralTelemetry.RecordTokenChoice(player, context, localResult.FinalOptions, localResult.SelectedRelic,
                localResult.RerollHistory.Count);
            return localResult;
        }

        var sessionKey = BuildSessionKey(AstralChoiceKind.RefreshableRelicSelection, player, context);
        var choiceId = synchronizer.ReserveChoiceId(player);
        if (IsLocalPlayer(runManager, player))
        {
            var result = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title, subtitlePrefix,
                probabilityText, rerollFunc);
            synchronizer.SyncLocalChoice(
                player,
                choiceId,
                CreateRefreshableRelicChoiceResult((RunState?)player.RunState, sessionKey, result));
            AstralTelemetry.RecordTokenChoice(player, context, result.FinalOptions, result.SelectedRelic,
                result.RerollHistory.Count);
            Log.Info(
                $"[{MainFile.ModId}] Synced local refreshable relic choice: context={context} player={player.NetId} choiceId={choiceId} index={result.SelectedIndex} rerolls={result.RerollHistory.Count}");
            return result;
        }

        var remoteChoice = await WaitForRemoteIndexedEnvelope(
            synchronizer,
            player,
            choiceId,
            AstralChoiceKind.RefreshableRelicSelection,
            (RunState?)player.RunState,
            sessionKey,
            context);
        if (remoteChoice == null ||
            !TryDecodeRefreshableRelicChoice(
                remoteChoice.Value.Payload,
                out var selectedIndex,
                out var rerollHistory))
        {
            Log.Warn(
                $"[{MainFile.ModId}] Failed to decode refreshable relic choice: context={context} player={player.NetId} choiceId={choiceId}");
            return CreateForcedRefreshableFallbackResult(options, rerollCount);
        }

        var finalOptions = rebuildFromHistory(rerollHistory);
        var selectedRelic = selectedIndex >= 0 && selectedIndex < finalOptions.Count
            ? finalOptions[selectedIndex]
            : null;
        AstralTelemetry.RecordTokenChoice(player, context, finalOptions, selectedRelic, rerollHistory.Count);
        Log.Info(
            $"[{MainFile.ModId}] Received remote refreshable relic choice: context={context} player={player.NetId} choiceId={choiceId} index={selectedIndex} rerolls={rerollHistory.Count}");
        return new RefreshableTokenRelicSelectionResult
        {
            SelectedRelic = selectedRelic,
            SelectedIndex = selectedIndex,
            StartingRerolls = rerollCount,
            RemainingRerolls = Math.Max(0, rerollCount - rerollHistory.Count),
            RerollHistory = rerollHistory,
            FinalOptions = finalOptions.ToList()
        };
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

    internal static async Task<RemoteIndexedChoiceEnvelope?> WaitForRemoteIndexedEnvelope(
        PlayerChoiceSynchronizer synchronizer,
        Player player,
        uint choiceId,
        AstralChoiceKind kind,
        RunState? runState,
        string sessionKey,
        string context)
    {
        return await WaitForRemoteIndexedEnvelopeAnyKind(
            synchronizer,
            player,
            choiceId,
            [kind],
            runState,
            sessionKey,
            context);
    }

    internal static async Task<RemoteIndexedChoiceEnvelope?> WaitForRemoteIndexedEnvelopeAnyKind(
        PlayerChoiceSynchronizer synchronizer,
        Player player,
        uint choiceId,
        IReadOnlyCollection<AstralChoiceKind> allowedKinds,
        RunState? runState,
        string sessionKey,
        string context)
    {
        for (var attempt = 0; attempt < MaxForeignChoiceMessagesPerChoice; attempt++)
        {
            var remoteChoice = await synchronizer.WaitForRemoteChoice(player, choiceId);
            if (AstralChoiceProtocol.TryDecodeIndexedEnvelopeHeader(
                    remoteChoice,
                    runState,
                    sessionKey,
                    out var actualKind,
                    out var sequence,
                    out var payload))
            {
                if (allowedKinds.Contains(actualKind))
                    return new RemoteIndexedChoiceEnvelope(remoteChoice, actualKind, sequence, payload);

                Log.Warn(
                    $"[{MainFile.ModId}] Ignored multiplayer choice with unexpected kind: context={context} player={player.NetId} choiceId={choiceId} kind={actualKind} allowed={string.Join(",", allowedKinds)} attempt={attempt + 1}/{MaxForeignChoiceMessagesPerChoice}.");
                continue;
            }

            Log.Warn(
                $"[{MainFile.ModId}] Ignored foreign multiplayer choice: context={context} player={player.NetId} choiceId={choiceId} allowed={string.Join(",", allowedKinds)} attempt={attempt + 1}/{MaxForeignChoiceMessagesPerChoice}.");
        }

        Log.Error(
            $"[{MainFile.ModId}] Exhausted remote multiplayer choice wait after repeated foreign payloads: context={context} player={player.NetId} choiceId={choiceId} allowed={string.Join(",", allowedKinds)}.");
        return null;
    }

    private static PlayerChoiceResult CreateRefreshableRelicChoiceResult(
        RunState? runState,
        string sessionKey,
        RefreshableTokenRelicSelectionResult result)
    {
        var payload = new List<int>(4 + result.RerollHistory.Count)
        {
            result.SelectedIndex,
            result.RerollHistory.Count
        };
        payload.AddRange(result.RerollHistory);
        return AstralChoiceProtocol.CreateIndexedEnvelope(
            AstralChoiceKind.RefreshableRelicSelection,
            runState,
            sessionKey,
            0,
            payload);
    }

    private static bool TryDecodeRefreshableRelicChoice(
        IReadOnlyList<int> payload,
        out int selectedIndex,
        out IReadOnlyList<int> rerollHistory)
    {
        selectedIndex = -1;
        rerollHistory = [];

        if (payload.Count < 2)
            return false;

        selectedIndex = payload[0];
        var rerollCount = Math.Max(0, payload[1]);
        if (payload.Count < rerollCount + 2)
            return false;

        rerollHistory = payload.Skip(2).Take(rerollCount).ToArray();
        return true;
    }

    private static bool IsLocalPlayer(RunManager runManager, Player player)
    {
        return player.NetId != 0UL && player.NetId == runManager.NetService.NetId;
    }

    private static async Task<RelicModel?> ShowLocalRelicSelection(Player player, IReadOnlyList<RelicModel> options)
    {
        var screen = NChooseARelicSelection.ShowScreen(options);
        if (screen == null)
            return null;

        if (LocalContext.IsMe(player))
            foreach (var relic in options)
                SaveManager.Instance?.MarkRelicAsSeen(relic);

        var selectedRelics = await screen.RelicsSelected() ?? [];
        return selectedRelics.FirstOrDefault();
    }

    private static async Task<RefreshableTokenRelicSelectionResult> ShowLocalRefreshableRelicSelection(
        Player player,
        IReadOnlyList<RelicModel> options,
        int rerollCount,
        string title,
        string subtitlePrefix,
        string probabilityText,
        Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> rerollFunc)
    {
        var overlayStack = await WaitForOverlayStackAsync();
        if (overlayStack == null)
            return CreateForcedRefreshableFallbackResult(options, rerollCount);

        if (LocalContext.IsMe(player))
            foreach (var relic in options)
                SaveManager.Instance?.MarkRelicAsSeen(relic);

        var screen = RefreshableTokenRelicSelectionScreen.Create(player, options, rerollCount, title, subtitlePrefix,
            probabilityText, rerollFunc);
        overlayStack.Push(screen);
        var result = await screen.WaitForResult();
        screen.Close();
        await screen.WaitUntilClosedAsync();
        await WaitForOverlaySettleFramesAsync(2);
        return result;
    }

    private static async Task<NOverlayStack?> WaitForOverlayStackAsync()
    {
        for (var i = 0; i < MaxSynchronizerWaitFrames; i++)
        {
            if (NOverlayStack.Instance != null)
                return NOverlayStack.Instance;

            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }

        return NOverlayStack.Instance;
    }

    // Rest-site callers are sensitive to overlays still being torn down when the selection result arrives.
    private static async Task WaitForOverlaySettleFramesAsync(int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            if (NGame.Instance?.IsInsideTree() == true)
                await NGame.Instance.ToSignal(NGame.Instance.GetTree(), SceneTree.SignalName.ProcessFrame);
            else
                await Task.Yield();
        }
    }

    private static RefreshableTokenRelicSelectionResult CreateForcedRefreshableFallbackResult(
        IReadOnlyList<RelicModel> options,
        int rerollCount)
    {
        var selectedRelic = options.FirstOrDefault();
        return new RefreshableTokenRelicSelectionResult
        {
            SelectedRelic = selectedRelic,
            SelectedIndex = selectedRelic == null ? -1 : 0,
            StartingRerolls = Math.Max(0, rerollCount),
            RemainingRerolls = Math.Max(0, rerollCount),
            RerollHistory = [],
            FinalOptions = options.ToList()
        };
    }

    private static async Task<CardModel?> ShowLocalCanonicalCardSelection(
        Player player,
        IReadOnlyList<CardModel> options,
        bool canSkip)
    {
        NPlayerHand.Instance?.CancelAllCardPlay();
        var displayOptions = CreateDisplayCardOptions(player, options);
        var screen = NChooseACardSelectionScreen.ShowScreen(displayOptions, canSkip);
        if (screen == null)
            return null;

        if (LocalContext.IsMe(player))
            foreach (var card in options)
                SaveManager.Instance?.MarkCardAsSeen(card.CanonicalInstance ?? card);

        var selectedCards = await screen.CardsSelected() ?? [];
        var selectedCard = selectedCards.FirstOrDefault();
        return selectedCard?.CanonicalInstance ?? selectedCard;
    }

    private static IReadOnlyList<CardModel> CreateDisplayCardOptions(Player player, IReadOnlyList<CardModel> options)
    {
        var displayOptions = new List<CardModel>(options.Count);
        foreach (var option in options)
        {
            var displayCard = CreateDisplayCardOption(player, option);
            displayOptions.Add(displayCard);
        }

        return displayOptions;
    }

    private static CardModel CreateDisplayCardOption(Player player, CardModel option)
    {
        var displayCard = (option.CanonicalInstance ?? option).ToMutable();
        displayCard.Owner = player;

        CopyDisplayUpgradeState(option, displayCard);
        return displayCard;
    }

    private static void CopyDisplayUpgradeState(CardModel source, CardModel displayCard)
    {
        while (displayCard.CurrentUpgradeLevel < source.CurrentUpgradeLevel)
        {
            displayCard.UpgradeInternal();
            displayCard.FinalizeUpgradeInternal();
        }
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

    private static int IndexOfCard(IReadOnlyList<CardModel> cards, CardModel card)
    {
        var targetId = (card.CanonicalInstance ?? card).Id;
        for (var i = 0; i < cards.Count; i++)
        {
            var candidateId = (cards[i].CanonicalInstance ?? cards[i]).Id;
            if (candidateId == targetId)
                return i;
        }

        return -1;
    }

    private static string BuildSessionKey(AstralChoiceKind kind, Player player, string context)
    {
        return $"{kind}|{context}|{player.NetId}";
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
