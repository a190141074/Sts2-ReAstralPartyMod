using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EventDeckCardHelper
{
    private static readonly string[] CardCollectionMemberNames =
    [
        "Cards",
        "_cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    public static List<CardModel> GetRunDeckCards(Player owner)
    {
        if (owner.RunState == null)
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Owner {owner.NetId} has no RunState; using Player.Deck fallback.");

        var cards = TryGetCardsFromPlayerDeck(owner);
        if (cards.Count > 0)
            return cards;

        MainFile.Logger.Warn($"[EventDeckCardHelper] Failed to locate readable run deck for owner {owner.NetId}.");
        return [];
    }

    public static List<CardModel> GetUpgradedCards(Player owner)
    {
        return GetRunDeckCards(owner)
            .Where(static card => card.CurrentUpgradeLevel > 0)
            .ToList();
    }

    public static List<CardModel> GetUpgradeableUnupgradedCards(Player owner)
    {
        return GetRunDeckCards(owner)
            .Where(static card => card.CurrentUpgradeLevel == 0 && card.IsUpgradable)
            .ToList();
    }

    public static async Task<bool> AddCardToRunDeckAsync(
        Player owner,
        CardModel card,
        bool showPreview = true,
        float previewTime = 2f)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(card);

        if (owner.RunState == null)
        {
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Failed to add card '{card.Id.Entry}' to run deck for owner {owner.NetId}: no RunState.");
            return false;
        }

        if (card.Owner != null && !ReferenceEquals(card.Owner, owner))
        {
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Refused to add card '{card.Id.Entry}' to owner {owner.NetId}: it already belongs to another owner.");
            return false;
        }

        if (!owner.RunState.ContainsCard(card))
            owner.RunState.AddCard(card, owner);

        return await AddOwnedCardToRunDeckAsync(owner, card, showPreview, previewTime) != null;
    }

    public static void AddCardToRunDeckSafely(
        Player owner,
        CardModel card,
        bool showPreview = true,
        float previewTime = 2f)
    {
        TaskHelper.RunSafely(AddCardToRunDeckAsync(owner, card, showPreview, previewTime));
    }

    public static async Task<bool> RemoveCardFromRunDeck(Player owner, CardModel card, bool showPreview = true)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(card);

        var beforeDeckCount = owner.Deck?.Cards.Count ?? -1;
        var removedFromDeck = false;
        if (card.Pile?.Type == PileType.Deck)
            try
            {
                await CardPileCmd.RemoveFromDeck(card, showPreview);
                removedFromDeck = owner.Deck?.Cards.Contains(card) != true;
            }
            catch (Exception ex)
            {
                MainFile.Logger.Warn(
                    $"[EventDeckCardHelper] CardPileCmd.RemoveFromDeck failed for '{card.Id.Entry}' owner {owner.NetId}: {ex.Message}");
            }

        if (!removedFromDeck) removedFromDeck = TryRemoveCardFromPlayerDeck(owner, card);

        var removedFromRunState = !owner.RunState?.ContainsCard(card) ?? true;
        if (!removedFromRunState)
            removedFromRunState = TryRemoveCardFromRunState(owner, card);

        var removed = removedFromDeck && removedFromRunState;

        var afterDeckCount = owner.Deck?.Cards.Count ?? -1;

        if (removed)
            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Removed card '{card.Id.Entry}' from run deck for owner {owner.NetId} | deckCount={beforeDeckCount}->{afterDeckCount}.");
        else
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Failed to remove card '{card.Id.Entry}' from run deck for owner {owner.NetId} | deckCount={beforeDeckCount}->{afterDeckCount}.");

        return removed;
    }

    public static void RemoveCardFromRunDeckSafely(Player owner, CardModel card, bool showPreview = true)
    {
        TaskHelper.RunSafely(RemoveCardFromRunDeck(owner, card, showPreview));
    }

    private static object? ReadMemberValue(object source, MemberInfo member)
    {
        return member switch
        {
            PropertyInfo propertyInfo => propertyInfo.GetValue(source),
            FieldInfo fieldInfo => fieldInfo.GetValue(source),
            _ => null
        };
    }

    private static List<CardModel> TryGetCardsFromPlayerDeck(Player owner)
    {
        var deck = owner.Deck;
        if (deck == null)
            return [];

        var cards = ExtractCards(deck.Cards, owner);
        if (cards.Count > 0)
        {
            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Resolved run deck via Player.Deck.Cards for owner {owner.NetId}.");
            return cards;
        }

        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            cards = ExtractCards(ReadMemberValue(deck, member), owner);
            if (cards.Count == 0)
                continue;

            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Resolved run deck via Player.Deck member '{memberName}' for owner {owner.NetId}.");
            return cards;
        }

        return [];
    }

    private static bool TryRemoveCardFromPlayerDeck(Player owner, CardModel card)
    {
        var deck = owner.Deck;
        if (deck == null)
            return false;

        if (!deck.Cards.Contains(card))
            return false;

        deck.RemoveInternal(card);
        return true;
    }

    private static bool TryRemoveCardFromRunState(Player owner, CardModel card)
    {
        var runState = owner.RunState;
        if (runState == null)
            return false;

        if (!ReferenceEquals(card.Owner, owner))
            return false;

        try
        {
            runState.RemoveCard(card);
            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] RunState.RemoveCard failed for '{card.Id.Entry}' owner {owner.NetId}: {ex.Message}");
            return false;
        }
    }

    private static List<CardModel> ExtractCards(object? source, Player owner)
    {
        if (source is not IEnumerable enumerable)
            return [];

        var cards = new List<CardModel>();
        foreach (var entry in enumerable)
        {
            if (entry is not CardModel card)
                continue;

            if (!ReferenceEquals(card.Owner, owner))
            {
                MainFile.Logger.Error(
                    $"[EventDeckCardHelper] Ignored invalid deck card | owner={owner.NetId} | card={card.Id.Entry}#{card.GetHashCode()} | cardOwner={card.Owner?.NetId.ToString() ?? "<null>"}.");
                continue;
            }

            cards.Add(card);
        }

        return cards;
    }

    private static async Task<CardModel?> AddOwnedCardToRunDeckAsync(
        Player owner,
        CardModel card,
        bool showPreview,
        float previewTime = 2f)
    {
        if (owner.RunState == null
            || owner.Deck == null
            || !ReferenceEquals(card.Owner, owner)
            || !owner.RunState.ContainsCard(card))
        {
            MainFile.Logger.Error(
                $"[EventDeckCardHelper] Refused invalid run-deck add | owner={owner.NetId} | card={card.Id.Entry}#{card.GetHashCode()} | cardOwner={card.Owner?.NetId.ToString() ?? "<null>"} | inRunState={owner.RunState?.ContainsCard(card) == true}.");
            return null;
        }

        var deckBefore = owner.Deck.Cards.Count;
        var result = await CardPileCmd.Add(card, owner.Deck, CardPilePosition.Bottom, null, false);
        var addedCard = result.cardAdded;
        var inTargetDeck = result.success
                           && ReferenceEquals(addedCard.Owner, owner)
                           && owner.Deck.Cards.Contains(addedCard)
                           && addedCard.Pile == owner.Deck;
        if (!inTargetDeck)
        {
            MainFile.Logger.Error(
                $"[EventDeckCardHelper] Run-deck add verification failed | owner={owner.NetId} | card={card.Id.Entry}#{card.GetHashCode()} | result={result.success} | resultCard={addedCard.Id.Entry}#{addedCard.GetHashCode()} | deck={deckBefore}->{owner.Deck.Cards.Count} | inTargetDeck={owner.Deck.Cards.Contains(addedCard)}.");
            if (owner.RunState.ContainsCard(card) && card.Pile != owner.Deck)
                owner.RunState.RemoveCard(card);
            return null;
        }

        // The pile mutation is synchronized, but the official preview belongs only to the player
        // whose deck received the card. Showing it to observers can leave their deck UI stale.
        if (showPreview && LocalContext.IsMe(owner))
            CardCmd.PreviewCardPileAdd(result, previewTime);

        if (LocalContext.IsMe(owner))
            SaveManager.Instance?.MarkCardAsSeen(addedCard.CanonicalInstance ?? addedCard);
        MainFile.Logger.Info(
            $"[EventDeckCardHelper] Added verified run-deck card | owner={owner.NetId} | card={addedCard.Id.Entry}#{addedCard.GetHashCode()} | inRunState={owner.RunState.ContainsCard(addedCard)} | deck={deckBefore}->{owner.Deck.Cards.Count}.");
        owner.GetRelic<TokenGoldVitamin>()?.HandleRunDeckCardAdded();
        return addedCard;
    }
}
