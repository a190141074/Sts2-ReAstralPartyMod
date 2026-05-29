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
        var cards = TryGetCardsFromPlayerDeck(owner);
        if (cards.Count > 0)
            return cards;

        if (owner.RunState == null)
        {
            MainFile.Logger.Warn($"[EventDeckCardHelper] Owner {owner.NetId} has no RunState and no readable deck.");
            return [];
        }

        var runStateType = owner.RunState.GetType();
        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = runStateType
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            var extractedCards = ExtractCards(ReadMemberValue(owner.RunState, member), owner);
            if (extractedCards.Count == 0)
                continue;

            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Resolved run deck via RunState member '{memberName}' for owner {owner.NetId}.");
            return extractedCards;
        }

        MainFile.Logger.Warn($"[EventDeckCardHelper] Failed to locate run deck container for owner {owner.NetId}.");
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

        var removedFromPlayerDeck = TryRemoveCardFromPlayerDeck(owner, card);
        var removedFromRunState = TryRemoveCardFromRunState(owner, card);
        var removed = removedFromPlayerDeck || removedFromRunState;

        if (removed)
        {
            MainFile.Logger.Info(
                $"[EventDeckCardHelper] Removed card '{card.Id.Entry}' from run deck for owner {owner.NetId} | playerDeck={removedFromPlayerDeck} | runState={removedFromRunState}.");
        }
        else
        {
            MainFile.Logger.Warn(
                $"[EventDeckCardHelper] Failed to remove card '{card.Id.Entry}' from run deck for owner {owner.NetId}.");
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

        var removed = RemoveCardFromCollection(deck.Cards, card);
        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            removed |= RemoveCardFromCollection(ReadMemberValue(deck, member), card);
        }

        return removed;
    }

    private static bool TryRemoveCardFromRunState(Player owner, CardModel card)
    {
        var runState = owner.RunState;
        if (runState == null)
            return false;

        var removed = false;
        var runStateType = runState.GetType();
        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = runStateType
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            removed |= RemoveCardFromCollection(ReadMemberValue(runState, member), card);
        }

        return removed;
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

    private static bool RemoveCardFromCollection(object? source, CardModel card)
    {
        if (source is not IList list)
            return false;

        var removed = false;
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(list[i], card))
                continue;

            list.RemoveAt(i);
            removed = true;
        }

        return removed;
    }
}
