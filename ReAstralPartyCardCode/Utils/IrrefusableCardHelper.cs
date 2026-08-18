using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class IrrefusableCardHelper
{
    private const string RunDataKey = "irrefusable_cards";
    private const int StandardCombatsRemaining = 3;
    private const int CurseCombatsRemaining = StandardCombatsRemaining + 2;
    private static readonly SavedAttachedState<CardModel, string> TransferId =
        new($"{MainFile.ModId}_irrefusable_transfer_id", static () => string.Empty);
    private static readonly HashSet<CardModel> PlayedFromHand = [];
    private static readonly HashSet<CardModel> DiscardedFromHand = [];
    private static RunSavedData<IrrefusableRunData> _runData = null!;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId).Register(
                RunDataKey,
                static () => new IrrefusableRunData(),
                new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });
        }

        RitsuLibFramework.GetContentRegistry(MainFile.ModId).RegisterSingleton<IrrefusableCardLifecycle>();
        _registered = true;
    }

    public static int QueueTransfers(Player source, Player target, IReadOnlyList<CardModel> cards)
    {
        if (source.RunState is not RunState runState || ReferenceEquals(source, target))
            return 0;

        var data = Get(runState);
        var queued = 0;
        foreach (var card in cards.Distinct())
        {
            if (!ReferenceEquals(card.Owner, source)
                || card.Pile?.Type != PileType.Deck
                || source.Deck?.Cards.Contains(card) != true)
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] queue rejected invalid source card | source={source.NetId} | target={target.NetId} | card={card.Id.Entry} | pile={card.Pile?.Type.ToString() ?? "<null>"}.");
                continue;
            }

            var existingTransferId = TransferId.GetValueOrDefault(card, string.Empty);
            if (!string.IsNullOrEmpty(existingTransferId)
                && data.PendingTransfers.Any(record => record.TransferId == existingTransferId))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] queue skipped already-pending card | transfer={existingTransferId} | source={source.NetId} | card={card.Id.Entry}.");
                continue;
            }

            RemoveExistingLifetimeRecord(data, source, card, existingTransferId);
            if (!card.Keywords.Contains(AstralKeywords.Irrefusable))
                card.AddKeyword(AstralKeywords.Irrefusable);

            var transferId = $"{source.NetId}:{target.NetId}:{++data.NextTransferSequence}";
            TransferId[card] = transferId;
            data.PendingTransfers.Add(new IrrefusablePendingTransfer
            {
                TransferId = transferId,
                SourcePlayerNetId = source.NetId,
                TargetPlayerNetId = target.NetId,
                Sequence = data.NextTransferSequence
            });
            queued++;

            MainFile.Logger.Info(
                $"[Irrefusable] queued transfer | transfer={transferId} | source={source.NetId} | target={target.NetId} | card={card.Id.Entry} | keyword={card.Keywords.Contains(AstralKeywords.Irrefusable)}.");
        }

        _runData.Set(runState, data);
        return queued;
    }

    public static void TrackHandPileExit(CardModel card, PileType oldPileType)
    {
        if (!card.Keywords.Contains(AstralKeywords.Irrefusable) || oldPileType != PileType.Hand)
            return;

        switch (card.Pile?.Type)
        {
            case PileType.Play:
                PlayedFromHand.Add(card);
                break;
            case PileType.Discard:
                DiscardedFromHand.Add(card);
                break;
        }
    }

    public static async Task HandleCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (!PlayedFromHand.Remove(card)
            || !card.Keywords.Contains(AstralKeywords.Irrefusable)
            || card.Owner?.Creature == null)
            return;

        await ConsumeAndGrantStarlight(choiceContext, card, "played");
    }

    public static async Task HandleCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (!DiscardedFromHand.Remove(card)
            || !card.Keywords.Contains(AstralKeywords.Irrefusable)
            || card.Owner?.Creature == null)
            return;

        await ConsumeAndGrantStarlight(choiceContext, card, "discarded");
    }

    public static bool HasIrrefusableCard(Player? owner)
    {
        return owner != null && EventDeckCardHelper.GetRunDeckCards(owner)
            .Any(card => card.Keywords.Contains(AstralKeywords.Irrefusable));
    }

    public static async Task HandleCombatEnd(CombatRoom room, RunState? runState)
    {
        PlayedFromHand.Clear();
        DiscardedFromHand.Clear();

        if (runState == null)
            return;

        var data = Get(runState);
        foreach (var pending in data.PendingTransfers
                     .OrderBy(record => record.Sequence)
                     .ThenBy(record => record.TransferId, StringComparer.Ordinal)
                     .ToList())
        {
            await ResolvePendingTransfer(runState, data, pending);
        }

        foreach (var record in data.Cards.ToList())
        {
            record.CombatsRemaining--;
            if (record.CombatsRemaining > 0)
                continue;

            var owner = runState.Players.FirstOrDefault(player => player.NetId == record.PlayerNetId);
            var card = owner == null ? null : ResolveDeckCard(owner, record);
            var removed = owner != null && card != null && await RemoveDeckCardOfficial(owner, card, "expiry");
            data.Cards.Remove(record);
            MainFile.Logger.Info(
                $"[Irrefusable] expired card | transfer={record.TransferId ?? "<legacy>"} | owner={record.PlayerNetId} | card={record.CardId} | removed={removed}.");
        }

        _runData.Set(runState, data);
    }

    private static async Task ResolvePendingTransfer(
        RunState runState,
        IrrefusableRunData data,
        IrrefusablePendingTransfer pending)
    {
        var source = runState.Players.FirstOrDefault(player => player.NetId == pending.SourcePlayerNetId);
        var target = runState.Players.FirstOrDefault(player => player.NetId == pending.TargetPlayerNetId);
        if (source == null || target == null || ReferenceEquals(source, target))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer retry waiting for players | transfer={pending.TransferId} | source={pending.SourcePlayerNetId} | target={pending.TargetPlayerNetId}.");
            return;
        }

        var sourceCard = FindDeckCardByTransferId(source, pending.TransferId);
        var targetCard = FindDeckCardByTransferId(target, pending.TransferId);
        if (targetCard != null)
        {
            if (sourceCard != null && !await RemoveDeckCardOfficial(source, sourceCard, "pending-completion"))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] transfer retry source removal failed | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId}.");
                return;
            }

            await CompletePendingTransfer(data, pending, source, target, targetCard, sourceCard == null);
            return;
        }

        if (sourceCard == null)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer retry source marker missing | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId}.");
            return;
        }

        try
        {
            var createdTarget = runState.LoadCard(sourceCard.ToSerializable(), target);
            var addResult = await CardPileCmd.Add(
                createdTarget,
                target.Deck,
                CardPilePosition.Bottom,
                null,
                true);
            if (!addResult.success
                || !ReferenceEquals(addResult.cardAdded.Owner, target)
                || target.Deck.Cards.Contains(addResult.cardAdded) != true
                || addResult.cardAdded.Pile != target.Deck
                || addResult.cardAdded.HasBeenRemovedFromState)
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] transfer retry target add failed | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId} | result={addResult.success}.");
                if (createdTarget.Pile == null && !createdTarget.HasBeenRemovedFromState)
                    createdTarget.RemoveFromState();
                return;
            }

            targetCard = addResult.cardAdded;
            MainFile.Logger.Info(
                $"[Irrefusable] transfer target added | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId} | card={targetCard.Id.Entry} | target_in_deck={target.Deck.Cards.Contains(targetCard)} | target_in_run_state={target.RunState?.ContainsCard(targetCard) == true} | target_removed={targetCard.HasBeenRemovedFromState} | target_deck={target.Deck.Cards.Count}.");
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error(
                $"[Irrefusable] transfer retry target creation failed | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId}: {exception}");
            return;
        }

        if (!await RemoveDeckCardOfficial(source, sourceCard, "transfer"))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer retry retained target after source removal failure | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId} | target_in_deck={target.Deck.Cards.Contains(targetCard!)}.");
            return;
        }

        await CompletePendingTransfer(data, pending, source, target, targetCard!, false);
    }

    private static async Task CompletePendingTransfer(
        IrrefusableRunData data,
        IrrefusablePendingTransfer pending,
        Player source,
        Player target,
        CardModel targetCard,
        bool alreadyApplied)
    {
        if (!targetCard.Keywords.Contains(AstralKeywords.Irrefusable))
            targetCard.AddKeyword(AstralKeywords.Irrefusable);

        TransferId[targetCard] = pending.TransferId;
        EnsureLifetimeRecord(data, target, targetCard, pending.TransferId);
        data.PendingTransfers.Remove(pending);

        if (!alreadyApplied)
        {
            var sourceRelic = source.GetRelic<VariantPersonShadowScion>();
            await CardGainAttribution.RunWithSource(sourceRelic, () =>
                GeneratedCardObserver.NotifyCardGained(targetCard, sourceRelic));
        }

        if (EventDeckCardHelper.GetRunDeckCards(source).Count == 0)
            await FallenHeavenlyKingDeckHelper.ReplaceWithHeavenlyKingDeck(source);

        MainFile.Logger.Info(
            $"[Irrefusable] transfer {(alreadyApplied ? "already applied" : "applied")} | transfer={pending.TransferId} | source={source.NetId} | target={target.NetId} | card={targetCard.Id.Entry} | source_deck={source.Deck?.Cards.Count ?? -1} | target_deck={target.Deck?.Cards.Count ?? -1}.");
    }

    private static void EnsureLifetimeRecord(
        IrrefusableRunData data,
        Player target,
        CardModel card,
        string transferId)
    {
        if (data.Cards.Any(record => record.TransferId == transferId))
            return;

        data.Cards.Add(new IrrefusableCardRecord
        {
            TransferId = transferId,
            PlayerNetId = target.NetId,
            DeckIndex = EventDeckCardHelper.GetRunDeckCards(target).IndexOf(card),
            CardId = (card.CanonicalInstance ?? card).Id.ToString(),
            UpgradeLevel = card.CurrentUpgradeLevel,
            FloorAddedToDeck = card.FloorAddedToDeck ?? 0,
            CombatsRemaining = card.Type == CardType.Curse
                ? CurseCombatsRemaining
                : StandardCombatsRemaining
        });
    }

    private static void RemoveExistingLifetimeRecord(
        IrrefusableRunData data,
        Player source,
        CardModel card,
        string existingTransferId)
    {
        if (!string.IsNullOrEmpty(existingTransferId))
            data.Cards.RemoveAll(record => record.TransferId == existingTransferId);

        data.Cards.RemoveAll(record =>
            record.PlayerNetId == source.NetId
            && string.IsNullOrEmpty(record.TransferId)
            && MatchesLegacyCard(card, record));
    }

    private static CardModel? FindDeckCardByTransferId(Player owner, string transferId)
    {
        return EventDeckCardHelper.GetRunDeckCards(owner)
            .FirstOrDefault(card => TransferId.GetValueOrDefault(card, string.Empty) == transferId);
    }

    private static async Task<bool> RemoveDeckCardOfficial(Player owner, CardModel card, string reason)
    {
        if (card.Pile?.Type != PileType.Deck)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] official deck removal skipped non-deck card | reason={reason} | owner={owner.NetId} | card={card.Id.Entry} | pile={card.Pile?.Type.ToString() ?? "<null>"}.");
            return false;
        }

        try
        {
            var sourceDeckBefore = owner.Deck?.Cards.Count ?? -1;
            await CardPileCmd.RemoveFromDeck(card, false);
            var removed = owner.Deck?.Cards.Contains(card) != true
                          && card.HasBeenRemovedFromState;
            MainFile.Logger.Info(
                $"[Irrefusable] official deck removal {(removed ? "succeeded" : "verification failed")} | reason={reason} | owner={owner.NetId} | card={card.Id.Entry} | deck={sourceDeckBefore}->{owner.Deck?.Cards.Count ?? -1} | in_deck={owner.Deck?.Cards.Contains(card) == true} | has_been_removed={card.HasBeenRemovedFromState} | in_run_state={owner.RunState?.ContainsCard(card) == true}.");
            if (!removed)
                MainFile.Logger.Warn(
                    $"[Irrefusable] official deck removal verification failed | reason={reason} | owner={owner.NetId} | card={card.Id.Entry}.");
            return removed;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error(
                $"[Irrefusable] official deck removal failed | reason={reason} | owner={owner.NetId} | card={card.Id.Entry}: {exception}");
            return false;
        }
    }

    private static async Task ConsumeAndGrantStarlight(
        PlayerChoiceContext choiceContext,
        CardModel card,
        string trigger)
    {
        var owner = card.Owner;
        if (owner?.Creature == null)
            return;

        await CardCmd.Exhaust(choiceContext, card, false, false);
        await TokenEternalStarlight.GrantStacks(owner, 3);
        MainFile.Logger.Info(
            $"[Irrefusable] {trigger} from hand and exhausted | owner={owner.NetId} | card={card.Id.Entry}");
    }

    private static IrrefusableRunData Get(RunState runState)
    {
        if (!_registered)
            Register();
        return _runData.TryGet(runState, out var data) && data != null
            ? data
            : new IrrefusableRunData();
    }

    private static CardModel? ResolveDeckCard(Player owner, IrrefusableCardRecord record)
    {
        var cards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (!string.IsNullOrEmpty(record.TransferId))
            return cards.FirstOrDefault(card => TransferId.GetValueOrDefault(card, string.Empty) == record.TransferId);

        if (record.DeckIndex >= 0 && record.DeckIndex < cards.Count)
        {
            var indexed = cards[record.DeckIndex];
            if (MatchesLegacyCard(indexed, record))
                return indexed;
        }

        return cards.FirstOrDefault(card => MatchesLegacyCard(card, record));
    }

    private static bool MatchesLegacyCard(CardModel card, IrrefusableCardRecord record)
    {
        return string.Equals((card.CanonicalInstance ?? card).Id.ToString(), record.CardId,
                   StringComparison.Ordinal)
               && card.CurrentUpgradeLevel == record.UpgradeLevel
               && (card.FloorAddedToDeck ?? 0) == record.FloorAddedToDeck
               && card.Keywords.Contains(AstralKeywords.Irrefusable);
    }
}

internal sealed class IrrefusableCardLifecycle : HookedSingletonModel
{
    public IrrefusableCardLifecycle() : base(HookType.Combat)
    {
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        return IrrefusableCardHelper.HandleCardPlayed(choiceContext, cardPlay);
    }

    public override Task AfterCardChangedPiles(
        CardModel card,
        PileType oldPileType,
        AbstractModel? clonedBy)
    {
        IrrefusableCardHelper.TrackHandPileExit(card, oldPileType);
        return Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        return IrrefusableCardHelper.HandleCardDiscarded(choiceContext, card);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return IrrefusableCardHelper.HandleCombatEnd(room, CurrentRunState as RunState);
    }
}

internal sealed class IrrefusableRunData
{
    public int NextTransferSequence { get; set; }
    public List<IrrefusablePendingTransfer> PendingTransfers { get; set; } = [];
    public List<IrrefusableCardRecord> Cards { get; set; } = [];
}

internal sealed class IrrefusablePendingTransfer
{
    public string TransferId { get; set; } = string.Empty;
    public ulong SourcePlayerNetId { get; set; }
    public ulong TargetPlayerNetId { get; set; }
    public int Sequence { get; set; }
}

internal sealed class IrrefusableCardRecord
{
    public string? TransferId { get; set; }
    public ulong PlayerNetId { get; set; }
    public int DeckIndex { get; set; }
    public string CardId { get; set; } = string.Empty;
    public int UpgradeLevel { get; set; }
    public int FloorAddedToDeck { get; set; }
    public int CombatsRemaining { get; set; }
}
