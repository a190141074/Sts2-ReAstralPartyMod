using System.Text;
using System.Text.Json;
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
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Models;
using STS2RitsuLib.Models.Capabilities;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class IrrefusableCardHelper
{
    private const string RunDataKey = "irrefusable_cards";
    private const int StandardCombatsRemaining = 3;
    private const int CurseCombatsRemaining = StandardCombatsRemaining + 2;
    private const string TransferActionKey = "irrefusable_pending_transfer_batch";
    private static readonly JsonSerializerOptions TransferSnapshotJsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<CardModel> PlayedFromHand = [];

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
        var transfers = new List<IrrefusablePendingTransfer>();
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

            if (FallenHeavenlyKingDeckHelper.IsHeavenlyKingDeckCard(card))
            {
                MainFile.Logger.Info(
                    $"[Irrefusable] queue rejected heavenly king deck card | source={source.NetId} | target={target.NetId} | card={card.Id.Entry}.");
                continue;
            }

            var originalDeckIndex = EventDeckCardHelper.GetRunDeckCards(source).IndexOf(card);
            if (originalDeckIndex < 0)
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] queue rejected source card without a readable deck index | source={source.NetId} | card={card.Id.Entry}.");
                continue;
            }

            if (IsSourceDeckIndexPending(data, source.NetId, originalDeckIndex)
                || transfers.Any(transfer => transfer.OriginalSourceDeckIndex == originalDeckIndex))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] queue skipped already-pending card | source={source.NetId} | deck_index={originalDeckIndex} | card={card.Id.Entry}.");
                continue;
            }

            var transferId = $"{source.NetId}:{target.NetId}:{++data.NextTransferSequence}";
            var snapshot = card.ToSerializable();
            var sourceDeckIndex = originalDeckIndex
                                  - CountEarlierPendingDeckRemovals(data, source.NetId, originalDeckIndex);
            transfers.Add(new IrrefusablePendingTransfer
            {
                TransferId = transferId,
                SourcePlayerNetId = source.NetId,
                TargetPlayerNetId = target.NetId,
                Sequence = data.NextTransferSequence,
                SourceDeckIndex = sourceDeckIndex,
                OriginalSourceDeckIndex = originalDeckIndex,
                CardSnapshot = snapshot
            });
        }

        if (transfers.Count == 0)
            return 0;

        var batch = new IrrefusablePendingTransferBatch
        {
            BatchId = $"{source.NetId}:{target.NetId}:{transfers[0].Sequence}",
            SourcePlayerNetId = source.NetId,
            TargetPlayerNetId = target.NetId,
            Transfers = transfers
        };
        data.PendingTransferBatches.Add(batch);
        _runData.Set(runState, data);
        MainFile.Logger.Info(
            $"[Irrefusable] queued transfer batch | batch={batch.BatchId} | source={source.NetId} | target={target.NetId} | count={transfers.Count} | transfers={string.Join(',', transfers.Select(transfer => $"{transfer.TransferId}@{transfer.SourceDeckIndex}"))}.");
        return transfers.Count;
    }

    public static async Task HandleHandPileExit(CardModel card, PileType oldPileType)
    {
        if (!card.Keywords.Contains(AstralKeywords.Irrefusable) || oldPileType != PileType.Hand)
            return;

        var destinationPile = card.Pile?.Type;
        if (destinationPile == PileType.Hand)
            return;

        MainFile.Logger.Info(
            $"[Irrefusable] hand exit detected | owner={card.Owner?.NetId.ToString() ?? "<null>"} | card={card.Id.Entry} | destination={destinationPile?.ToString() ?? "<none>"}.");
        if (destinationPile == PileType.Play)
        {
            PlayedFromHand.Add(card);
            return;
        }

        await ConsumeAndGrantStarlight(new ThrowingPlayerChoiceContext(), card, "left hand");
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

    public static bool HasIrrefusableCard(Player? owner)
    {
        return owner != null && EventDeckCardHelper.GetRunDeckCards(owner)
            .Any(card => card.Keywords.Contains(AstralKeywords.Irrefusable));
    }

    public static bool HasPendingTransferBatchForSource(RunState runState, ulong sourcePlayerNetId)
    {
        return Get(runState).PendingTransferBatches.Any(batch =>
            batch.SourcePlayerNetId == sourcePlayerNetId
            && batch.Transfers.Count > 0);
    }

    public static Task HandleCombatEnd(CombatRoom room, RunState? runState)
    {
        PlayedFromHand.Clear();

        if (runState == null)
            return Task.CompletedTask;

        var data = Get(runState);
        foreach (var batch in data.PendingTransferBatches
                     .Where(batch => batch.Transfers.Count > 0)
                     .OrderBy(batch => batch.Transfers.Min(transfer => transfer.Sequence))
                     .ThenBy(batch => batch.BatchId, StringComparer.Ordinal)
                     .ToList())
            QueuePendingTransferBatchAction(runState, batch);

        _runData.Set(runState, data);
        return Task.CompletedTask;
    }

    private static void QueuePendingTransferBatchAction(
        RunState runState,
        IrrefusablePendingTransferBatch batch)
    {
        var source = runState.Players.FirstOrDefault(player => player.NetId == batch.SourcePlayerNetId);
        if (source == null || batch.Transfers.Count == 0 ||
            batch.Transfers.Any(transfer => transfer.CardSnapshot == null))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer batch retained | batch={batch.BatchId} | reason={(source == null ? "source_missing" : "snapshot_missing")}.");
            return;
        }

        if (!LocalContext.IsMe(source))
            return;

        var payload = new IrrefusableTransferActionPayload
        {
            BatchId = batch.BatchId,
            SourcePlayerNetId = batch.SourcePlayerNetId,
            TargetPlayerNetId = batch.TargetPlayerNetId,
            Transfers = batch.Transfers
                .OrderBy(transfer => transfer.Sequence)
                .ThenBy(transfer => transfer.TransferId, StringComparer.Ordinal)
                .ToList()
        };

        if (!RitsuLibManagedNetActions.Request(RunManager.Instance, TransferAction, payload, source.NetId))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] transfer batch retained | batch={batch.BatchId} | source={source.NetId} | target={batch.TargetPlayerNetId} | reason=managed_action_unavailable.");
            return;
        }

        MainFile.Logger.Info(
            $"[Irrefusable] managed transfer batch requested | batch={batch.BatchId} | source={source.NetId} | target={batch.TargetPlayerNetId} | count={payload.Transfers.Count}.");
    }

    private static async Task ExecuteTransferAction(
        RitsuLibManagedNetActionContext<IrrefusableTransferActionPayload> context)
    {
        var payload = context.Message;
        var runState = context.Player.RunState as RunState ?? RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=run_state_unavailable.");
            return;
        }

        var data = Get(runState);
        var batch = data.PendingTransferBatches.FirstOrDefault(record => record.BatchId == payload.BatchId);
        if (batch == null)
        {
            MainFile.Logger.Info(
                $"[Irrefusable] managed transfer batch already settled | batch={payload.BatchId} | source={payload.SourcePlayerNetId} | target={payload.TargetPlayerNetId}.");
            return;
        }

        if (payload.SourcePlayerNetId != batch.SourcePlayerNetId
            || payload.TargetPlayerNetId != batch.TargetPlayerNetId
            || payload.Transfers.Count != batch.Transfers.Count
            || payload.Transfers.Any(transfer => transfer.CardSnapshot == null)
            || !PayloadMatchesBatch(payload.Transfers, batch.Transfers))
        {
            MainFile.Logger.Error(
                $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=payload_mismatch.");
            _runData.Set(runState, data);
            return;
        }

        var source = runState.Players.FirstOrDefault(player => player.NetId == batch.SourcePlayerNetId);
        var target = runState.Players.FirstOrDefault(player => player.NetId == batch.TargetPlayerNetId);
        if (source == null || target == null || ReferenceEquals(source, target))
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=player_missing | source={batch.SourcePlayerNetId} | target={batch.TargetPlayerNetId}.");
            _runData.Set(runState, data);
            return;
        }

        MainFile.Logger.Info(
            $"[Irrefusable] managed transfer batch executing | batch={payload.BatchId} | source={source.NetId} | target={target.NetId} | count={payload.Transfers.Count} | source_deck={source.Deck?.Cards.Count ?? -1} | target_deck={target.Deck?.Cards.Count ?? -1}.");

        var orderedTransfers = payload.Transfers
            .Select(transfer => new
            {
                Transfer = transfer,
                Original = batch.Transfers.FirstOrDefault(item => item.TransferId == transfer.TransferId)
                    ?.OriginalSourceDeckIndex ?? int.MaxValue
            })
            .OrderBy(item => item.Original)
            .ThenBy(item => item.Transfer.Sequence)
            .ThenBy(item => item.Transfer.TransferId, StringComparer.Ordinal)
            .Select(item => item.Transfer)
            .ToList();

        foreach (var transfer in orderedTransfers)
        {
            if (batch.AppliedTransferIds.Contains(transfer.TransferId))
                continue;

            var sourceDeckIndex = transfer.SourceDeckIndex
                                  - batch.Transfers.Count(item =>
                                      item.OriginalSourceDeckIndex < transfer.OriginalSourceDeckIndex);
            var sourceCard = FindDeckCardByIndex(source, sourceDeckIndex);
            var targetCard = FindTargetDeckCardByTransferId(target, transfer.TransferId);
            if (sourceCard != null && !MatchesTransferSnapshot(sourceCard, transfer.CardSnapshot!))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | transfer={transfer.TransferId} | reason=source_snapshot_mismatch | deck_index={sourceDeckIndex} | card={sourceCard.Id.Entry}.");
                _runData.Set(runState, data);
                return;
            }

            if (targetCard == null && sourceCard == null)
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | transfer={transfer.TransferId} | reason=source_missing | deck_index={sourceDeckIndex}.");
                _runData.Set(runState, data);
                return;
            }

            try
            {
                if (targetCard == null)
                {
                    var createdTarget = runState.LoadCard(transfer.CardSnapshot!, target);
                    if (!await EventDeckCardHelper.AddCardToRunDeckAsync(target, createdTarget, true))
                    {
                        MainFile.Logger.Warn(
                            $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | transfer={transfer.TransferId} | reason=target_add_failed.");
                        _runData.Set(runState, data);
                        return;
                    }

                    if (!createdTarget.Keywords.Contains(AstralKeywords.Irrefusable))
                        createdTarget.AddKeyword(AstralKeywords.Irrefusable);
                    EnsureLifetimeCapability(createdTarget, transfer.TransferId);
                    targetCard = createdTarget;
                }
            }
            catch (Exception exception)
            {
                MainFile.Logger.Error(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | transfer={transfer.TransferId} | reason=target_creation_failed | error={exception}");
                _runData.Set(runState, data);
                return;
            }

            if (sourceCard != null && !await RemoveDeckCardOfficial(source, sourceCard, "managed-transfer"))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | transfer={transfer.TransferId} | reason=source_remove_failed.");
                _runData.Set(runState, data);
                return;
            }

            batch.AppliedTransferIds.Add(transfer.TransferId);
            MainFile.Logger.Info(
                $"[Irrefusable] managed transfer batch item applied | batch={payload.BatchId} | transfer={transfer.TransferId} | card={targetCard.Id.Entry} | source_deck={source.Deck?.Cards.Count ?? -1} | target_deck={target.Deck?.Cards.Count ?? -1}.");
        }

        if (batch.AppliedTransferIds.Count != batch.Transfers.Count)
        {
            MainFile.Logger.Warn(
                $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=incomplete | applied={batch.AppliedTransferIds.Count}/{batch.Transfers.Count}.");
            _runData.Set(runState, data);
            return;
        }

        var fallenHeavenlyKing = source.GetRelic<PersonalityDerivativeFallenHeavenlyKing>();
        if (!batch.HeavenlyKingDeckReplacementPending
            && fallenHeavenlyKing != null
            && EventDeckCardHelper.GetRunDeckCards(source).Count == 0)
        {
            batch.HeavenlyKingDeckReplacementPending = true;
            MainFile.Logger.Info(
                $"[Irrefusable] heavenly deck replacement pending | batch={payload.BatchId} | source={source.NetId} | reason=source_deck_empty.");
        }

        if (batch.HeavenlyKingDeckReplacementPending)
        {
            if (fallenHeavenlyKing == null)
            {
                MainFile.Logger.Error(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=fallen_heavenly_king_derivative_missing.");
                _runData.Set(runState, data);
                return;
            }

            if (!await FallenHeavenlyKingDeckHelper.EnsureHeavenlyKingDeckAsync(source))
            {
                MainFile.Logger.Warn(
                    $"[Irrefusable] managed transfer batch retained | batch={payload.BatchId} | reason=heavenly_deck_replacement_incomplete.");
                _runData.Set(runState, data);
                return;
            }

            fallenHeavenlyKing.AstralParty_FallenHeavenlyKingDeckReplaced = true;
            MainFile.Logger.Info(
                $"[Irrefusable] heavenly deck replacement completed | batch={payload.BatchId} | source={source.NetId} | cards={source.Deck?.Cards.Count ?? -1}.");
        }

        if (!batch.StarlightGranted)
        {
            source.GetRelic<VariantPersonShadowScion>()
                ?.AccumulatePendingTransferredStarlight(batch.AppliedTransferIds.Count);
            batch.StarlightGranted = true;
        }

        data.PendingTransferBatches.Remove(batch);
        MainFile.Logger.Info(
            $"[Irrefusable] managed transfer batch applied | batch={payload.BatchId} | source={source.NetId} | target={target.NetId} | count={payload.Transfers.Count} | source_deck={source.Deck?.Cards.Count ?? -1} | target_deck={target.Deck?.Cards.Count ?? -1}.");
        _runData.Set(runState, data);
    }

    private static void EnsureLifetimeCapability(CardModel card, string transferId)
    {
        var totalCombats = card.Type == CardType.Curse
            ? CurseCombatsRemaining
            : StandardCombatsRemaining;
        var lifetime = card.GetOrCreateCapability<IrrefusableLifetimeCapability>();
        lifetime.Initialize(transferId, totalCombats, 1);
    }

    private static byte[] SerializeTransferAction(IrrefusableTransferActionPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        WriteTransferString(writer, payload.BatchId, "batch id");
        writer.WriteULong(payload.SourcePlayerNetId);
        writer.WriteULong(payload.TargetPlayerNetId);
        writer.WriteInt(payload.Transfers.Count);
        EnsureTransferPayloadWithinLimit(writer);
        foreach (var transfer in payload.Transfers.OrderBy(transfer => transfer.Sequence)
                     .ThenBy(transfer => transfer.TransferId, StringComparer.Ordinal))
        {
            ArgumentNullException.ThrowIfNull(transfer.CardSnapshot);
            WriteTransferString(writer, transfer.TransferId, "transfer id");
            writer.WriteInt(transfer.Sequence);
            writer.WriteInt(transfer.SourceDeckIndex);
            writer.WriteInt(transfer.OriginalSourceDeckIndex);
            writer.Write(transfer.CardSnapshot);
            EnsureTransferPayloadWithinLimit(writer);
        }

        EnsureTransferPayloadWithinLimit(writer);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static IrrefusableTransferActionPayload DeserializeTransferAction(ReadOnlySpan<byte> payload)
    {
        var reader = new PacketReader();
        reader.Reset([.. payload]);
        var actionPayload = new IrrefusableTransferActionPayload
        {
            BatchId = ReadTransferString(reader, "batch id"),
            SourcePlayerNetId = reader.ReadULong(),
            TargetPlayerNetId = reader.ReadULong()
        };

        var count = reader.ReadInt();
        if (count <= 0 || count > 4)
            throw new InvalidOperationException($"Invalid Irrefusable transfer batch count: {count}.");

        for (var index = 0; index < count; index++)
            actionPayload.Transfers.Add(new IrrefusablePendingTransfer
            {
                TransferId = ReadTransferString(reader, "transfer id"),
                Sequence = reader.ReadInt(),
                SourceDeckIndex = reader.ReadInt(),
                OriginalSourceDeckIndex = reader.ReadInt(),
                CardSnapshot = reader.Read<SerializableCard>()
            });

        return actionPayload;
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
            throw new InvalidOperationException($"Invalid Irrefusable {fieldName} length: {byteCount}.");

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

    private static bool IsSourceDeckIndexPending(IrrefusableRunData data, ulong sourcePlayerNetId,
        int originalDeckIndex)
    {
        return data.PendingTransferBatches.Any(batch => batch.Transfers.Any(transfer =>
            transfer.SourcePlayerNetId == sourcePlayerNetId
            && transfer.OriginalSourceDeckIndex == originalDeckIndex));
    }

    private static int CountEarlierPendingDeckRemovals(IrrefusableRunData data, ulong sourcePlayerNetId,
        int originalDeckIndex)
    {
        return data.PendingTransferBatches
            .SelectMany(batch => batch.Transfers)
            .Count(transfer => transfer.SourcePlayerNetId == sourcePlayerNetId
                               && transfer.OriginalSourceDeckIndex < originalDeckIndex);
    }

    private static bool PayloadMatchesBatch(
        IReadOnlyCollection<IrrefusablePendingTransfer> payloadTransfers,
        IReadOnlyCollection<IrrefusablePendingTransfer> batchTransfers)
    {
        return payloadTransfers
            .OrderBy(transfer => transfer.Sequence)
            .ThenBy(transfer => transfer.TransferId, StringComparer.Ordinal)
            .Select(transfer => (transfer.TransferId, transfer.Sequence, transfer.SourceDeckIndex,
                transfer.OriginalSourceDeckIndex))
            .SequenceEqual(batchTransfers
                .OrderBy(transfer => transfer.Sequence)
                .ThenBy(transfer => transfer.TransferId, StringComparer.Ordinal)
                .Select(transfer => (transfer.TransferId, transfer.Sequence, transfer.SourceDeckIndex,
                    transfer.OriginalSourceDeckIndex)));
    }

    private static CardModel? FindDeckCardByIndex(Player owner, int deckIndex)
    {
        var cards = EventDeckCardHelper.GetRunDeckCards(owner);
        return deckIndex >= 0 && deckIndex < cards.Count
            ? cards[deckIndex]
            : null;
    }

    private static CardModel? FindTargetDeckCardByTransferId(Player owner, string transferId)
    {
        return EventDeckCardHelper.GetRunDeckCards(owner)
            .FirstOrDefault(card => card.TryGetCapability<IrrefusableLifetimeCapability>(out var lifetime)
                                    && lifetime.MatchesTransferId(transferId));
    }

    private static bool MatchesTransferSnapshot(CardModel card, SerializableCard snapshot)
    {
        return JsonSerializer.Serialize(card.ToSerializable(), TransferSnapshotJsonOptions)
               == JsonSerializer.Serialize(snapshot, TransferSnapshotJsonOptions);
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

        if (card.Pile?.Type != PileType.Exhaust)
            await CardCmd.Exhaust(choiceContext, card, false, false);

        await PowerCmd.Apply<EffectsStarlightPower>(owner.Creature, 3m, owner.Creature, card, false);
        MainFile.Logger.Info(
            $"[Irrefusable] {trigger} resolved | owner={owner.NetId} | card={card.Id.Entry} | starlight=3.");
    }

    private static IrrefusableRunData Get(RunState runState)
    {
        if (!_registered)
            Register();
        return _runData.TryGet(runState, out var data) && data != null
            ? data
            : new IrrefusableRunData();
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
        return IrrefusableCardHelper.HandleHandPileExit(card, oldPileType);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        return IrrefusableCardHelper.HandleCombatEnd(room, CurrentRunState as RunState);
    }
}

internal sealed class IrrefusableRunData
{
    public int NextTransferSequence { get; set; }
    public List<IrrefusablePendingTransferBatch> PendingTransferBatches { get; set; } = [];
}

internal sealed class IrrefusablePendingTransfer
{
    public string TransferId { get; set; } = string.Empty;
    public ulong SourcePlayerNetId { get; set; }
    public ulong TargetPlayerNetId { get; set; }
    public int Sequence { get; set; }
    public int SourceDeckIndex { get; set; }
    public int OriginalSourceDeckIndex { get; set; }
    public SerializableCard? CardSnapshot { get; set; }
}

internal sealed class IrrefusablePendingTransferBatch
{
    public string BatchId { get; set; } = string.Empty;
    public ulong SourcePlayerNetId { get; set; }
    public ulong TargetPlayerNetId { get; set; }
    public List<IrrefusablePendingTransfer> Transfers { get; set; } = [];
    public List<string> AppliedTransferIds { get; set; } = [];
    public bool HeavenlyKingDeckReplacementPending { get; set; }
    public bool StarlightGranted { get; set; }
}

internal sealed class IrrefusableTransferActionPayload
{
    public string BatchId { get; set; } = string.Empty;
    public ulong SourcePlayerNetId { get; set; }
    public ulong TargetPlayerNetId { get; set; }
    public List<IrrefusablePendingTransfer> Transfers { get; set; } = [];
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

    internal bool MatchesTransferId(string transferId)
    {
        return string.Equals(_transferId, transferId, StringComparison.Ordinal);
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
