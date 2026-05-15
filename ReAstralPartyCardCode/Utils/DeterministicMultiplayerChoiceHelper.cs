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
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class DeterministicMultiplayerChoiceHelper
{
    private const int MaxSynchronizerWaitFrames = 60;
    private const int ChoiceMagic = unchecked((int)0x5241504D);
    private const int ChoiceKindRelicSelection = 1;
    private const int ChoiceKindCanonicalCardSelection = 2;
    private const int ChoiceKindRefreshableRelicSelection = 3;

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
            AstralTelemetry.RecordTokenChoice(player, context, options, selectedRelic, 0);
            Log.Info($"[{MainFile.ModId}] Synced local relic choice: context={context} player={player.NetId} choiceId={choiceId} index={selectedIndex}");
            return selectedRelic;
        }

        var remoteChoice = await WaitForRemoteChoice(synchronizer, player, choiceId, context);
        var remoteIndex = DecodeRelicChoiceIndex(remoteChoice);
        var remoteRelic = remoteIndex >= 0 && remoteIndex < options.Count ? options[remoteIndex] : null;
        AstralTelemetry.RecordTokenChoice(player, context, options, remoteRelic, 0);
        Log.Info($"[{MainFile.ModId}] Received remote relic choice: context={context} player={player.NetId} choiceId={choiceId} index={remoteIndex}");
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
            var localResult = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title, subtitlePrefix, probabilityText, rerollFunc);
            AstralTelemetry.RecordTokenChoice(player, context, localResult.FinalOptions, localResult.SelectedRelic, localResult.RerollHistory.Count);
            return localResult;
        }

        var synchronizer = await WaitForPlayerChoiceSynchronizerAsync(runManager);
        if (synchronizer == null)
        {
            var localResult = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title, subtitlePrefix, probabilityText, rerollFunc);
            AstralTelemetry.RecordTokenChoice(player, context, localResult.FinalOptions, localResult.SelectedRelic, localResult.RerollHistory.Count);
            return localResult;
        }

        var choiceId = synchronizer.ReserveChoiceId(player);
        if (IsLocalPlayer(runManager, player))
        {
            var result = await ShowLocalRefreshableRelicSelection(player, options, rerollCount, title, subtitlePrefix, probabilityText, rerollFunc);
            synchronizer.SyncLocalChoice(player, choiceId, CreateRefreshableRelicChoiceResult(result));
            AstralTelemetry.RecordTokenChoice(player, context, result.FinalOptions, result.SelectedRelic, result.RerollHistory.Count);
            Log.Info(
                $"[{MainFile.ModId}] Synced local refreshable relic choice: context={context} player={player.NetId} choiceId={choiceId} index={result.SelectedIndex} rerolls={result.RerollHistory.Count}");
            return result;
        }

        var remoteChoice = await WaitForRefreshableRelicChoice(synchronizer, player, choiceId, context);
        if (!TryDecodeRefreshableRelicChoice(remoteChoice, out var selectedIndex, out var rerollHistory))
        {
            Log.Warn(
                $"[{MainFile.ModId}] Failed to decode refreshable relic choice: context={context} player={player.NetId} choiceId={choiceId}");
            return new RefreshableTokenRelicSelectionResult
            {
                SelectedRelic = null,
                SelectedIndex = -1,
                StartingRerolls = rerollCount,
                RemainingRerolls = rerollCount,
                RerollHistory = [],
                FinalOptions = options.ToList()
            };
        }

        var finalOptions = rebuildFromHistory(rerollHistory);
        var selectedRelic = selectedIndex >= 0 && selectedIndex < finalOptions.Count ? finalOptions[selectedIndex] : null;
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

    private static async Task<PlayerChoiceResult> WaitForRefreshableRelicChoice(
        PlayerChoiceSynchronizer synchronizer,
        Player player,
        uint initialChoiceId,
        string context)
    {
        var choiceId = initialChoiceId;
        while (true)
        {
            var remoteChoice = await synchronizer.WaitForRemoteChoice(player, choiceId);
            if (TryDecodeRefreshableRelicChoice(remoteChoice, out _, out _))
                return remoteChoice;

            Log.Warn($"[{MainFile.ModId}] Skipped non-refreshable relic multiplayer choice: context={context} player={player.NetId} choiceId={choiceId} result={remoteChoice}");
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

    private static PlayerChoiceResult CreateRefreshableRelicChoiceResult(RefreshableTokenRelicSelectionResult result)
    {
        var payload = new List<int>(4 + result.RerollHistory.Count)
        {
            ChoiceMagic,
            ChoiceKindRefreshableRelicSelection,
            result.SelectedIndex,
            result.RerollHistory.Count
        };
        payload.AddRange(result.RerollHistory);
        return PlayerChoiceResult.FromIndexes(payload);
    }

    private static bool TryDecodeRefreshableRelicChoice(
        PlayerChoiceResult result,
        out int selectedIndex,
        out IReadOnlyList<int> rerollHistory)
    {
        selectedIndex = -1;
        rerollHistory = [];
        try
        {
            var payload = result.AsIndexes();
            if (payload == null || payload.Count < 4)
                return false;
            if (payload[0] != ChoiceMagic || payload[1] != ChoiceKindRefreshableRelicSelection)
                return false;

            selectedIndex = payload[2];
            var rerollCount = Math.Max(0, payload[3]);
            if (payload.Count < rerollCount + 4)
                return false;

            rerollHistory = payload.Skip(4).Take(rerollCount).ToArray();
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

    private static async Task<RefreshableTokenRelicSelectionResult> ShowLocalRefreshableRelicSelection(
        Player player,
        IReadOnlyList<RelicModel> options,
        int rerollCount,
        string title,
        string subtitlePrefix,
        string probabilityText,
        Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> rerollFunc)
    {
        var overlayStack = NOverlayStack.Instance;
        if (overlayStack == null)
        {
            return new RefreshableTokenRelicSelectionResult
            {
                SelectedRelic = null,
                SelectedIndex = -1,
                StartingRerolls = rerollCount,
                RemainingRerolls = rerollCount,
                RerollHistory = [],
                FinalOptions = options.ToList()
            };
        }

        if (LocalContext.IsMe(player))
        {
            foreach (var relic in options)
                SaveManager.Instance.MarkRelicAsSeen(relic);
        }

        var screen = RefreshableTokenRelicSelectionScreen.Create(player, options, rerollCount, title, subtitlePrefix, probabilityText, rerollFunc);
        overlayStack.Push(screen);
        return await screen.WaitForResult();
    }

    private static async Task<CardModel?> ShowLocalCanonicalCardSelection(
        Player player,
        IReadOnlyList<CardModel> options,
        bool canSkip)
    {
        NPlayerHand.Instance?.CancelAllCardPlay();
        var displayOptions = CreateDisplayCardOptions(player, options);
        var screen = NChooseACardSelectionScreen.ShowScreen(displayOptions, canSkip);
        if (LocalContext.IsMe(player))
        {
            foreach (var card in options)
                SaveManager.Instance.MarkCardAsSeen(card.CanonicalInstance ?? card);
        }

        var selectedCard = (await screen.CardsSelected()).FirstOrDefault();
        return selectedCard?.CanonicalInstance ?? selectedCard;
    }

    private static IReadOnlyList<CardModel> CreateDisplayCardOptions(Player player, IReadOnlyList<CardModel> options)
    {
        var displayOptions = new List<CardModel>(options.Count);
        foreach (var option in options)
        {
            var displayCard = option.CanonicalInstance == null ? option.ToMutable() : option;
            displayCard.Owner ??= player;
            displayOptions.Add(displayCard);
        }

        return displayOptions;
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
