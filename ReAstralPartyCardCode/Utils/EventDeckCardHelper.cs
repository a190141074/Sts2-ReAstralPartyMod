using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Players;

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
            MainFile.Logger.Warn($"[EventDeckCardHelper] Owner {owner.NetId} has no RunState; using Player.Deck fallback.");

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

    public static bool RemoveCardFromRunDeck(Player owner, CardModel card)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(card);

        var beforeDeckCount = owner.Deck?.Cards.Count ?? -1;
        var removedFromPlayerDeck = TryRemoveCardFromPlayerDeck(owner, card);
        var removedFromRunState = TryRemoveCardFromRunState(owner, card);
        var afterDeckCount = owner.Deck?.Cards.Count ?? -1;
        var removed = removedFromPlayerDeck || removedFromRunState;

        if (removed)
        {
            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Removed card '{card.Id.Entry}' from run deck for owner {owner.NetId} | playerDeck={removedFromPlayerDeck} | runState={removedFromRunState} | deckCount={beforeDeckCount}->{afterDeckCount}.");
        }
        else
        {
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Failed to remove card '{card.Id.Entry}' from run deck for owner {owner.NetId} | deckCount={beforeDeckCount}->{afterDeckCount}.");
        }

        return removed;
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
            MainFile.Logger.Info($"[EventDeckCardHelper] Resolved run deck via Player.Deck.Cards for owner {owner.NetId}.");
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

            if (card.Owner == null)
                card.Owner = owner;

            cards.Add(card);
        }

        return cards;
    }
}
