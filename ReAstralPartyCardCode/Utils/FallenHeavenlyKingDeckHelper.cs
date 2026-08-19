using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.Networking.ManagedActions;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class FallenHeavenlyKingDeckHelper
{
    private const string ReplacementRunDataKey = "fallen_heavenly_king_deck_replacement";
    private const string ReplacementActionKey = "fallen_heavenly_king_deck_replacement";
    private static readonly HashSet<ulong> ReplacementRequestsInFlight = [];
    private static readonly RitsuLibManagedNetActionDescriptor<FallenHeavenlyKingDeckReplacementActionPayload>
        ReplacementAction =
            new(
                MainFile.ModId,
                ReplacementActionKey,
                SerializeReplacementAction,
                DeserializeReplacementAction,
                ExecuteReplacementAction,
                GameActionType.NonCombat);
    private static RunSavedData<FallenHeavenlyKingDeckReplacementRunData> _replacementRunData = null!;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _replacementRunData = RitsuLibFramework.GetRunSavedDataStore(MainFile.ModId).Register(
                ReplacementRunDataKey,
                static () => new FallenHeavenlyKingDeckReplacementRunData(),
                new RunSavedDataOptions { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault });
        }

        RitsuLibManagedNetActions.Register(ReplacementAction);
        RitsuLibFramework.SubscribeLifecycle<RoomEnteredEvent>(OnRoomEntered, replayCurrentState: false);
        RitsuLibFramework.SubscribeLifecycle<RunLoadedEvent>(OnRunLoaded, replayCurrentState: false);
        _registered = true;
    }

    public static async Task DuplicateDeckTwice(Player owner)
    {
        var runState = owner.RunState
                       ?? throw new InvalidOperationException(
                           $"Cannot duplicate the deck for player {owner.NetId}: no run state is available.");
        var originals = EventDeckCardHelper.GetRunDeckCards(owner)
            .Select(card => (CardId: card.Id.Entry, Snapshot: card.ToSerializable()))
            .ToList();
        foreach (var source in originals)
        {
            for (var copyIndex = 0; copyIndex < 2; copyIndex++)
            {
                try
                {
                    var copy = runState.LoadCard(source.Snapshot, owner);
                    if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, copy, true))
                    {
                        throw new InvalidOperationException(
                            $"Could not add duplicated card '{source.CardId}' to player {owner.NetId}'s deck.");
                    }
                }
                catch (Exception exception)
                {
                    MainFile.Logger.Error(
                        $"[FallenHeavenlyKing] deck duplication failed | owner={owner.NetId} | card={source.CardId} | copy={copyIndex + 1}: {exception}");
                    throw;
                }
            }
        }

        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] duplicated deck twice | owner={owner.NetId} | originals={originals.Count}");
    }

    public static async Task<bool> EnsureHeavenlyKingDeckAsync(Player owner)
    {
        var existingCards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (existingCards.Any(card => !IsHeavenlyKingDeckCard(card)))
        {
            MainFile.Logger.Error(
                $"[FallenHeavenlyKing] heavenly deck replacement blocked | owner={owner.NetId} | reason=unexpected_card | cards={DescribeDeck(existingCards)}.");
            return false;
        }

        if (!await AddMissingCopies<HkMajestyOfTheHeavenlyKing>(owner, 3, existingCards.Count(static card => card is HkMajestyOfTheHeavenlyKing))
            || !await AddMissingCopies<HkExclusiveChannel>(owner, 3, existingCards.Count(static card => card is HkExclusiveChannel))
            || !await AddMissingCopies<HkHeavenlyKingsPresence>(owner, 3, existingCards.Count(static card => card is HkHeavenlyKingsPresence))
            || !await AddMissingCopies<HkFallOfTheHeavenlyKing>(owner, 1, existingCards.Count(static card => card is HkFallOfTheHeavenlyKing))
            || !await AddMissingCopies<HkHeavenlyKingFallIncident>(owner, 1, existingCards.Count(static card => card is HkHeavenlyKingFallIncident)))
        {
            MainFile.Logger.Warn(
                $"[FallenHeavenlyKing] heavenly deck replacement retained | owner={owner.NetId} | cards={DescribeDeck(EventDeckCardHelper.GetRunDeckCards(owner))}.");
            return false;
        }

        var completedCards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (completedCards.Count != 11
            || completedCards.Count(static card => card is HkMajestyOfTheHeavenlyKing) != 3
            || completedCards.Count(static card => card is HkExclusiveChannel) != 3
            || completedCards.Count(static card => card is HkHeavenlyKingsPresence) != 3
            || completedCards.Count(static card => card is HkFallOfTheHeavenlyKing) != 1
            || completedCards.Count(static card => card is HkHeavenlyKingFallIncident) != 1
            || completedCards.Any(card => !IsHeavenlyKingDeckCard(card)))
        {
            MainFile.Logger.Error(
                $"[FallenHeavenlyKing] heavenly deck replacement retained | owner={owner.NetId} | reason=verification_failed | cards={DescribeDeck(completedCards)}.");
            return false;
        }

        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] heavenly deck replacement verified | owner={owner.NetId} | cards={DescribeDeck(completedCards)}.");
        return true;
    }

    public static void OnRunDeckRemovalCompleted(RunState runState, ulong ownerNetId)
    {
        var owner = runState.Players.FirstOrDefault(player => player.NetId == ownerNetId);
        if (owner == null
            || owner.Creature?.CombatState != null
            || owner.GetRelic<PersonalityDerivativeFallenHeavenlyKing>() == null)
            return;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (deckCards.Count != 0)
            return;

        if (IrrefusableCardHelper.HasPendingTransferBatchForSource(runState, owner.NetId))
        {
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] zero-deck replacement deferred to Irrefusable batch | owner={owner.NetId}.");
            return;
        }

        var data = GetReplacementData(runState);
        if (!data.PendingOwnerNetIds.Contains(owner.NetId))
        {
            data.PendingOwnerNetIds.Add(owner.NetId);
            _replacementRunData.Set(runState, data);
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] zero-deck replacement queued | owner={owner.NetId} | reason=outside_combat_deck_removed.");
        }

        RequestPendingReplacementAction(runState, owner, "deck_removed");
    }

    private static void OnRoomEntered(RoomEnteredEvent roomEnteredEvent)
    {
        if (roomEnteredEvent.RunState is RunState runState)
            RequestPendingReplacementActions(runState, "room_entered");
    }

    private static void OnRunLoaded(RunLoadedEvent runLoadedEvent)
    {
        if (runLoadedEvent.RunState is RunState runState)
            RequestPendingReplacementActions(runState, "run_loaded");
    }

    private static void RequestPendingReplacementActions(RunState runState, string reason)
    {
        foreach (var ownerNetId in GetReplacementData(runState).PendingOwnerNetIds
                     .Distinct()
                     .OrderBy(netId => netId)
                     .ToList())
        {
            var owner = runState.Players.FirstOrDefault(player => player.NetId == ownerNetId);
            if (owner == null)
            {
                MainFile.Logger.Warn(
                    $"[FallenHeavenlyKing] pending zero-deck replacement retained | owner={ownerNetId} | reason=owner_missing.");
                continue;
            }

            RequestPendingReplacementAction(runState, owner, reason);
        }
    }

    private static void RequestPendingReplacementAction(RunState runState, Player owner, string reason)
    {
        if (!LocalContext.IsMe(owner)
            || !ReplacementRequestsInFlight.Add(owner.NetId))
            return;

        var requested = RitsuLibManagedNetActions.Request(
            RunManager.Instance,
            ReplacementAction,
            new FallenHeavenlyKingDeckReplacementActionPayload { OwnerNetId = owner.NetId },
            owner.NetId);
        if (!requested)
        {
            ReplacementRequestsInFlight.Remove(owner.NetId);
            MainFile.Logger.Warn(
                $"[FallenHeavenlyKing] pending zero-deck replacement retained | owner={owner.NetId} | reason={reason}_managed_action_unavailable.");
            return;
        }

        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] managed zero-deck replacement requested | owner={owner.NetId} | reason={reason}.");
    }

    private static async Task ExecuteReplacementAction(
        RitsuLibManagedNetActionContext<FallenHeavenlyKingDeckReplacementActionPayload> context)
    {
        var ownerNetId = context.Message.OwnerNetId;
        ReplacementRequestsInFlight.Remove(ownerNetId);

        var runState = context.Player.RunState as RunState ?? RunManager.Instance?.DebugOnlyGetState();
        if (runState == null)
        {
            MainFile.Logger.Warn(
                $"[FallenHeavenlyKing] pending zero-deck replacement retained | owner={ownerNetId} | reason=run_state_unavailable.");
            return;
        }

        var data = GetReplacementData(runState);
        if (!data.PendingOwnerNetIds.Contains(ownerNetId))
        {
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] managed zero-deck replacement already settled | owner={ownerNetId}.");
            return;
        }

        var owner = runState.Players.FirstOrDefault(player => player.NetId == ownerNetId);
        var derivative = owner?.GetRelic<PersonalityDerivativeFallenHeavenlyKing>();
        if (owner == null || derivative == null)
        {
            data.PendingOwnerNetIds.Remove(ownerNetId);
            _replacementRunData.Set(runState, data);
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] managed zero-deck replacement cancelled | owner={ownerNetId} | reason=owner_or_derivative_missing.");
            return;
        }

        if (IrrefusableCardHelper.HasPendingTransferBatchForSource(runState, ownerNetId))
        {
            data.PendingOwnerNetIds.Remove(ownerNetId);
            _replacementRunData.Set(runState, data);
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] managed zero-deck replacement deferred to Irrefusable batch | owner={ownerNetId}.");
            return;
        }

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (deckCards.Count > 0 && deckCards.Any(card => !IsHeavenlyKingDeckCard(card)))
        {
            data.PendingOwnerNetIds.Remove(ownerNetId);
            _replacementRunData.Set(runState, data);
            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] managed zero-deck replacement cancelled | owner={ownerNetId} | reason=deck_no_longer_empty | cards={DescribeDeck(deckCards)}.");
            return;
        }

        if (!await EnsureHeavenlyKingDeckAsync(owner))
        {
            _replacementRunData.Set(runState, data);
            MainFile.Logger.Warn(
                $"[FallenHeavenlyKing] managed zero-deck replacement retained | owner={ownerNetId} | reason=deck_ensure_failed.");
            return;
        }

        derivative.AstralParty_FallenHeavenlyKingDeckReplaced = true;
        data.PendingOwnerNetIds.Remove(ownerNetId);
        _replacementRunData.Set(runState, data);
        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] managed zero-deck replacement completed | owner={ownerNetId} | cards={DescribeDeck(EventDeckCardHelper.GetRunDeckCards(owner))}.");
    }

    private static byte[] SerializeReplacementAction(FallenHeavenlyKingDeckReplacementActionPayload payload)
    {
        var writer = new PacketWriter { WarnOnGrow = false };
        writer.WriteULong(payload.OwnerNetId);
        writer.ZeroByteRemainder();
        return [.. writer.Buffer.AsSpan(0, writer.BytePosition)];
    }

    private static FallenHeavenlyKingDeckReplacementActionPayload DeserializeReplacementAction(ReadOnlySpan<byte> payload)
    {
        var reader = new PacketReader();
        reader.Reset([.. payload]);
        return new FallenHeavenlyKingDeckReplacementActionPayload { OwnerNetId = reader.ReadULong() };
    }

    private static FallenHeavenlyKingDeckReplacementRunData GetReplacementData(RunState runState)
    {
        return _replacementRunData.Get(runState);
    }

    public static void ReduceFallCost(Player owner, int amount)
    {
        if (amount <= 0)
            return;

        var deckCard = EventDeckCardHelper.GetRunDeckCards(owner).OfType<HkFallOfTheHeavenlyKing>().FirstOrDefault();
        if (deckCard == null)
            return;

        var nextCost = Math.Max(0, deckCard.EnergyCost.GetWithModifiers(CostModifiers.None) - amount);
        deckCard.EnergyCost.SetCustomBaseCost(nextCost);

        foreach (var combatCard in PileType.Hand.GetPile(owner).Cards.OfType<HkFallOfTheHeavenlyKing>())
            combatCard.EnergyCost.SetCustomBaseCost(nextCost);
    }

    private static async Task<bool> AddMissingCopies<TCard>(Player owner, int requiredCount, int existingCount)
        where TCard : CardModel
    {
        var missingCount = requiredCount - existingCount;
        if (missingCount <= 0)
            return true;

        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] heavenly deck adding missing cards | owner={owner.NetId} | card={typeof(TCard).Name} | existing={existingCount} | required={requiredCount} | missing={missingCount}.");
        for (var index = 0; index < missingCount; index++)
        {
            var card = ModelDb.Card<TCard>().ToMutable();
            card.FloorAddedToDeck = Math.Max(owner.RunState?.TotalFloor ?? 1, 1);
            if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, card, showPreview: true))
            {
                MainFile.Logger.Warn(
                    $"[FallenHeavenlyKing] heavenly deck add failed | owner={owner.NetId} | card={card.Id.Entry} | added={index}/{missingCount}.");
                return false;
            }

            MainFile.Logger.Info(
                $"[FallenHeavenlyKing] heavenly deck card added | owner={owner.NetId} | card={card.Id.Entry} | added={index + 1}/{missingCount}.");
        }

        return true;
    }

    public static bool IsHeavenlyKingDeckCard(CardModel card)
    {
        return card is HkMajestyOfTheHeavenlyKing
            or HkExclusiveChannel
            or HkHeavenlyKingsPresence
            or HkFallOfTheHeavenlyKing
            or HkHeavenlyKingFallIncident;
    }

    private static string DescribeDeck(IReadOnlyCollection<CardModel> cards)
    {
        return $"total={cards.Count},majesty={cards.Count(static card => card is HkMajestyOfTheHeavenlyKing)},exclusive_channel={cards.Count(static card => card is HkExclusiveChannel)},presence={cards.Count(static card => card is HkHeavenlyKingsPresence)},fall={cards.Count(static card => card is HkFallOfTheHeavenlyKing)},incident={cards.Count(static card => card is HkHeavenlyKingFallIncident)}";
    }
}

internal sealed class FallenHeavenlyKingDeckReplacementRunData
{
    public List<ulong> PendingOwnerNetIds { get; set; } = [];
}

internal sealed class FallenHeavenlyKingDeckReplacementActionPayload
{
    public ulong OwnerNetId { get; set; }
}
