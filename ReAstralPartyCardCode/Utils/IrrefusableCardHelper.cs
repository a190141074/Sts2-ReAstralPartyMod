using System.Text;
using System.Text.Json.Nodes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class IrrefusableCardHelper
{
    private const string RunDataKey = "irrefusable_cards";
    private const int StandardCombatsRemaining = 3;
    private const int CurseCombatsRemaining = StandardCombatsRemaining + 2;
    private const string TransferActionKey = "irrefusable_pending_transfer";
    private static readonly SavedAttachedState<CardModel, string> TransferId =
        new($"{MainFile.ModId}_irrefusable_transfer_id", static () => string.Empty);
    private static readonly HashSet<CardModel> PlayedFromHand = [];
    private static readonly HashSet<CardModel> DiscardedFromHand = [];
    private static readonly RitsuLibManagedNetActionDescriptor<IrrefusableTransferActionPayload> TransferAction =
        new(
            MainFile.ModId,
            TransferActionKey,
            SerializeTransferAction,
            DeserializeTransferAction,
            ExecuteTransferAction,
            GameActionType.NonCombat);
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
        RitsuLibFramework.GetContentRegistry(MainFile.ModId).RegisterModelCapability<IrrefusableLifetimeCapability>();
        RitsuLibManagedNetActions.Register(TransferAction);
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
            var snapshot = card.ToSerializable();
            data.PendingTransfers.Add(new IrrefusablePendingTransfer
            {
                TransferId = transferId,
                SourcePlayerNetId = source.NetId,
                TargetPlayerNetId = target.NetId,
                Sequence = data.NextTransferSequence,
                CardSnapshot = snapshot
            });
            queued++;

            MainFile.Logger.Info(
                $"[Irrefusable] queued transfer | transfer={transferId} | source={source.NetId} | target={target.NetId} | card={card.Id.Entry} | keyword={card.Keywords.Contains(AstralKeywords.Irrefusable)} | snapshot_saved=true.");
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
            QueuePendingTransferAction(runState, data, pending);
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

    private static void QueuePendingTransferAction(
        RunState runState,
        IrrefusableRunData data,
        IrrefusablePendingTransfer pending)
    {
        var source = runState.Players.FirstOrDefault(player => player.NetId == pending.SourcePlayerNetId);
        if (source == null || pending.CardSnapshot == null)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer request retained | transfer={pending.TransferId} | reason={(source == null ? "source_missing" : "snapshot_missing")}.");
            return;
        }

        if (!LocalContext.IsMe(source))
            return;

        if (pending.ActionRequested)
            return;

        var payload = new IrrefusableTransferActionPayload
        {
            TransferId = pending.TransferId,
            SourcePlayerNetId = pending.SourcePlayerNetId,
            TargetPlayerNetId = pending.TargetPlayerNetId,
            Sequence = pending.Sequence,
            CardSnapshot = pending.CardSnapshot
        };

        if (!RitsuLibManagedNetActions.Request(RunManager.Instance, TransferAction, payload, source.NetId))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer request retained | transfer={pending.TransferId} | source={source.NetId} | target={pending.TargetPlayerNetId} | reason=managed_action_unavailable.");
            return;
        }

        pending.ActionRequested = true;
        _runData.Set(runState, data);
        MainFile.Logger.Info(
            $"[Irrefusable] managed transfer action requested | transfer={pending.TransferId} | source={source.NetId} | target={pending.TargetPlayerNetId} | sequence={pending.Sequence}.");
    }

    private static async Task ExecuteTransferAction(RitsuLibManagedNetActionContext<IrrefusableTransferActionPayload> context)
    {
        var payload = context.Message;
        var runState = context.Player.RunState as RunState ?? RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=run_state_unavailable.");
            return;
        }

        var data = Get(runState);
        var pending = data.PendingTransfers.FirstOrDefault(record => record.TransferId == payload.TransferId);
        if (pending == null)
        {
            MainFile.Logger.Info(
                $"[Irrefusable] managed transfer action already settled | transfer={payload.TransferId} | source={payload.SourcePlayerNetId} | target={payload.TargetPlayerNetId}.");
            return;
        }

        pending.ActionRequested = false;
        if (payload.SourcePlayerNetId != pending.SourcePlayerNetId
            || payload.TargetPlayerNetId != pending.TargetPlayerNetId
            || payload.Sequence != pending.Sequence
            || payload.CardSnapshot == null)
        {
            MainFile.Logger.Error(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=payload_mismatch.");
            _runData.Set(runState, data);
            return;
        }

        var source = runState.Players.FirstOrDefault(player => player.NetId == pending.SourcePlayerNetId);
        var target = runState.Players.FirstOrDefault(player => player.NetId == pending.TargetPlayerNetId);
        if (source == null || target == null || ReferenceEquals(source, target))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=player_missing | source={pending.SourcePlayerNetId} | target={pending.TargetPlayerNetId}.");
            _runData.Set(runState, data);
            return;
        }

        MainFile.Logger.Info(
            $"[Irrefusable] managed transfer action executing | transfer={payload.TransferId} | source={source.NetId} | target={target.NetId} | source_deck={source.Deck?.Cards.Count ?? -1} | target_deck={target.Deck?.Cards.Count ?? -1}.");

        var sourceCard = FindDeckCardByTransferId(source, payload.TransferId);
        var targetCard = FindDeckCardByTransferId(target, payload.TransferId);
        if (targetCard == null && sourceCard == null)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=source_marker_missing.");
            _runData.Set(runState, data);
            return;
        }

        try
        {
            if (targetCard == null)
            {
                var createdTarget = runState.LoadCard(payload.CardSnapshot, target);
                var previewShown = LocalContext.IsMe(target);
                if (!await EventDeckCardHelper.AddCardToRunDeckAsync(target, createdTarget, true))
                {
                    MainFile.Logger.Warn(
                        $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=target_add_failed | preview_local={previewShown}.");
                    _runData.Set(runState, data);
                    return;
                }

                targetCard = FindDeckCardByTransferId(target, payload.TransferId);
                if (targetCard == null)
                {
                    MainFile.Logger.Error(
                        $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=target_marker_missing_after_add.");
                    _runData.Set(runState, data);
                    return;
                }

                MainFile.Logger.Info(
                    $"[Irrefusable] managed transfer target added | transfer={payload.TransferId} | target={target.NetId} | card={targetCard.Id.Entry} | preview_local={previewShown} | target_deck={target.Deck?.Cards.Count ?? -1}.");
            }
        }
        catch (Exception exception)
        {
            MainFile.Logger.Error(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=target_creation_failed | error={exception}");
            _runData.Set(runState, data);
            return;
        }

        if (sourceCard != null && !await RemoveDeckCardOfficial(source, sourceCard, "managed-transfer"))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer action retained | transfer={payload.TransferId} | reason=source_remove_failed | source={source.NetId} | target={target.NetId}.");
            _runData.Set(runState, data);
            return;
        }

        await CompletePendingTransfer(data, pending, source, target, targetCard, sourceCard == null);
        _runData.Set(runState, data);
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
        EnsureLifetimeCapability(data, target, targetCard, pending.TransferId);
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

    private static void EnsureLifetimeCapability(
        IrrefusableRunData data,
        Player target,
        CardModel card,
        string transferId)
    {
        // New transfers follow Guilty's card-owned lifecycle. Retain only old records so
        // saves created before this change still finish through the previous fallback.
        data.Cards.RemoveAll(record => record.TransferId == transferId);

        var totalCombats = card.Type == CardType.Curse
            ? CurseCombatsRemaining
            : StandardCombatsRemaining;
        var lifetime = card.GetOrCreateCapability<IrrefusableLifetimeCapability>();
        lifetime.Initialize(transferId, totalCombats, combatsSeen: 1);
    }

    private static byte[] SerializeTransferAction(IrrefusableTransferActionPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload.CardSnapshot);

        var writer = new PacketWriter { WarnOnGrow = false };
        WriteTransferString(writer, payload.TransferId, "transfer id");
        writer.WriteULong(payload.SourcePlayerNetId);
        writer.WriteULong(payload.TargetPlayerNetId);
        writer.WriteInt(payload.Sequence);
        writer.Write(payload.CardSnapshot);
        EnsureTransferPayloadWithinLimit(writer);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static IrrefusableTransferActionPayload DeserializeTransferAction(ReadOnlySpan<byte> payload)
    {
        var reader = new PacketReader();
        reader.Reset([.. payload]);
        return new IrrefusableTransferActionPayload
        {
            TransferId = ReadTransferString(reader, "transfer id"),
            SourcePlayerNetId = reader.ReadULong(),
            TargetPlayerNetId = reader.ReadULong(),
            Sequence = reader.ReadInt(),
            CardSnapshot = reader.Read<SerializableCard>()
        };
    }

    private static void WriteTransferString(PacketWriter writer, string value, string fieldName)
    {
        var byteCount = Encoding.UTF8.GetByteCount(value);
        if (byteCount > RitsuLibManagedNetActions.MaxPayloadBytes)
            throw new InvalidOperationException(
                $"Irrefusable {fieldName} exceeds {RitsuLibManagedNetActions.MaxPayloadBytes} bytes.");

        writer.WriteString(value);
        EnsureTransferPayloadWithinLimit(writer);
    }

    private static string ReadTransferString(PacketReader reader, string fieldName)
    {
        var remainingBits = reader.Buffer.Length * 8 - reader.BitPosition;
        if (remainingBits < 32)
            throw new InvalidOperationException($"Missing Irrefusable {fieldName} length.");

        var byteCount = reader.ReadInt();
        if (byteCount < 0
            || byteCount > RitsuLibManagedNetActions.MaxPayloadBytes
            || reader.Buffer.Length * 8 - reader.BitPosition < (long)byteCount * 8)
        {
            throw new InvalidOperationException($"Invalid Irrefusable {fieldName} length: {byteCount}.");
        }

        var bytes = new byte[byteCount];
        reader.ReadBytes(bytes, byteCount);
        return Encoding.UTF8.GetString(bytes);
    }

    private static void EnsureTransferPayloadWithinLimit(PacketWriter writer)
    {
        if (writer.BytePosition > RitsuLibManagedNetActions.MaxPayloadBytes)
            throw new InvalidOperationException(
                $"Irrefusable managed transfer payload exceeds {RitsuLibManagedNetActions.MaxPayloadBytes} bytes.");
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
    public SerializableCard? CardSnapshot { get; set; }
    public bool ActionRequested { get; set; }
}

public sealed class IrrefusableTransferActionPayload
{
    public string TransferId { get; set; } = string.Empty;
    public ulong SourcePlayerNetId { get; set; }
    public ulong TargetPlayerNetId { get; set; }
    public int Sequence { get; set; }
    public SerializableCard? CardSnapshot { get; set; }
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

public sealed class IrrefusableLifetimeCapability : CardCapability
{
    private const string TransferIdStateKey = "transfer_id";
    private const string CombatsSeenStateKey = "combats_seen";
    private const string TotalCombatsStateKey = "total_combats";

    private string _transferId = string.Empty;
    private int _combatsSeen;
    private int _totalCombats;

    public void Initialize(string transferId, int totalCombats, int combatsSeen)
    {
        if (string.IsNullOrWhiteSpace(transferId))
            throw new ArgumentException("Transfer ID is required.", nameof(transferId));

        _transferId = transferId;
        _totalCombats = totalCombats;
        _combatsSeen = combatsSeen;
        MarkDirty();
    }

    public override async Task AfterCombatEnd(CombatRoom _)
    {
        var card = Owner;
        if (card?.Pile?.Type != PileType.Deck || _totalCombats <= 0)
            return;

        _combatsSeen++;
        MarkDirty();
        if (_combatsSeen < _totalCombats || card.Pile?.Type != PileType.Deck)
            return;

        await CardPileCmd.RemoveFromDeck(card);
        MainFile.Logger.Info(
            $"[Irrefusable] Guilty-style expiry removed card | transfer={_transferId} | owner={card.Owner?.NetId.ToString() ?? "<null>"} | card={card.Id.Entry} | combats={_combatsSeen}/{_totalCombats}.");
    }

    protected override JsonNode? SaveAdditionalState()
    {
        return new JsonObject
        {
            [TransferIdStateKey] = _transferId,
            [CombatsSeenStateKey] = _combatsSeen,
            [TotalCombatsStateKey] = _totalCombats
        };
    }

    protected override void LoadAdditionalState(JsonNode? state, int schemaVersion)
    {
        if (state is not JsonObject data)
            return;

        _transferId = data[TransferIdStateKey]?.GetValue<string>() ?? string.Empty;
        _combatsSeen = data[CombatsSeenStateKey]?.GetValue<int>() ?? 0;
        _totalCombats = data[TotalCombatsStateKey]?.GetValue<int>() ?? 0;
    }
}
