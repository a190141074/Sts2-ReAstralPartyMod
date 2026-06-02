using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EnigmaticAcknowledgmentDeckHelper
{
    private const string AscendersBaneEntry = "ASCENDERS_BANE";

    private static readonly string[] CardCollectionMemberNames =
    [
        "Cards",
        "_cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    private static readonly string[] RunDeckMemberNames =
    [
        "Deck",
        "Cards",
        "CardModels",
        "MasterDeck",
        "StartingDeck"
    ];

    public static bool EnsureInRunDeck(Player owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        if (deckCards.Any(IsAcknowledgmentCard))
        {
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] already_present | owner={owner.NetId}");
            return true;
        }

        var ascendersBane = deckCards.FirstOrDefault(IsAscendersBane);
        if (ascendersBane != null && !EventDeckCardHelper.RemoveCardFromRunDeck(owner, ascendersBane))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] remove_failed | owner={owner.NetId} | card={ascendersBane.Id.Entry}");
            return false;
        }

        if (ascendersBane != null)
        {
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] removed_ascenders_bane | owner={owner.NetId} | floor={ascendersBane.FloorAddedToDeck}");
        }

        var mutableCard = CreateMutable(owner, ascendersBane);
        if (!TryAddCardToRunDeck(owner, mutableCard))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_failed | owner={owner.NetId} | card={mutableCard.Id.Entry}");
            return false;
        }

        SaveManager.Instance?.MarkCardAsSeen(mutableCard.CanonicalInstance ?? mutableCard);
        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] added_acknowledgment | owner={owner.NetId} | replacedAscendersBane={ascendersBane != null}");
        return true;
    }

    public static bool IsAcknowledgmentCard(CardModel? card)
    {
        if (card == null)
            return false;

        var targetId = ModelDb.Card<EnigmaticTheAcknowledgment>().Id;
        return card is EnigmaticTheAcknowledgment
               || card.Id == targetId
               || card.CanonicalInstance?.Id == targetId;
    }

    private static CardModel CreateMutable(Player owner, CardModel? replacedCard = null)
    {
        var mutableCard = ModelDb.Card<EnigmaticTheAcknowledgment>().ToMutable();
        mutableCard.Owner = owner;
        mutableCard.FloorAddedToDeck = replacedCard?.FloorAddedToDeck ?? 1;
        return mutableCard;
    }

    private static bool IsAscendersBane(CardModel? card)
    {
        return card != null && string.Equals(card.Id.Entry, AscendersBaneEntry, StringComparison.Ordinal);
    }

    private static bool TryAddCardToRunDeck(Player owner, CardModel card)
    {
        if (owner.RunState == null)
        {
            var addedWithoutRunState = TryAddCardToPlayerDeckCompatibility(owner, card);
            if (!addedWithoutRunState)
                MainFile.Logger.Warn($"[EnigmaticAcknowledgmentDeckHelper] Failed to add card without RunState | owner={owner.NetId}");
            return addedWithoutRunState;
        }

        var runStateType = owner.RunState.GetType();
        foreach (var memberName in RunDeckMemberNames)
        {
            var member = runStateType.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(owner.RunState, member) is not IList list)
                continue;

            list.Add(card);
            SyncPlayerDeck(owner, card);
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] Added card to run deck | owner={owner.NetId} | member={memberName} | runDeckCount={list.Count} | playerDeckCount={owner.Deck?.Cards.Count ?? 0}");
            return true;
        }

        var addedViaCompatibility = TryAddCardToPlayerDeckCompatibility(owner, card);
        if (addedViaCompatibility)
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] Added card via Player.Deck compatibility path | owner={owner.NetId} | playerDeckCount={owner.Deck?.Cards.Count ?? 0}");
            return true;
        }

        MainFile.Logger.Warn($"[EnigmaticAcknowledgmentDeckHelper] Failed to locate run deck container for owner {owner.NetId}.");
        return false;
    }

    private static void SyncPlayerDeck(Player owner, CardModel card)
    {
        if (owner.Deck?.Cards == null)
            return;

        if (owner.Deck.Cards.Contains(card))
            return;

        owner.Deck.AddInternal(card);
    }

    private static bool TryAddCardToPlayerDeckCompatibility(Player owner, CardModel card)
    {
        var deck = owner.Deck;
        if (deck == null)
            return false;

        if (TryAddCardToList(deck.Cards, card))
            return true;

        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (TryAddCardToList(ReadMemberValue(deck, member), card))
                return true;
        }

        deck.AddInternal(card);
        return deck.Cards.Contains(card);
    }

    private static bool TryAddCardToList(object? source, CardModel card)
    {
        if (source is not IList list)
            return false;

        if (list.Contains(card))
            return true;

        list.Add(card);
        return list.Contains(card);
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
}
