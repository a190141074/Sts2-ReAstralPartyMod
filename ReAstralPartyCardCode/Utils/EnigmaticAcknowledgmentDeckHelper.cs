using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal enum EnigmaticRevelationKind
{
    None = 0,
    Acknowledgment = 1,
    Twist = 2,
    Infinitum = 3
}

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
        if (deckCards.Any(IsRevelationCard))
        {
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] revelation_already_present | owner={owner.NetId}");
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

        var mutableCard = CreateMutable<EnigmaticTheAcknowledgment>(owner, ascendersBane);
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

    public static bool ReplaceAcknowledgmentWithTwist(Player? owner)
    {
        if (owner == null)
            return false;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var acknowledgment = deckCards.FirstOrDefault(IsAcknowledgmentCard);
        if (acknowledgment != null)
        {
            if (!EventDeckCardHelper.RemoveCardFromRunDeck(owner, acknowledgment))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] remove_acknowledgment_failed | owner={owner.NetId}");
                return false;
            }

            var twist = CreateMutable<EnigmaticTheTwist>(owner, acknowledgment);
            if (!TryAddCardToRunDeck(owner, twist))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] add_twist_failed | owner={owner.NetId}");
                return false;
            }

            SaveManager.Instance?.MarkCardAsSeen(twist.CanonicalInstance ?? twist);
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] replaced_acknowledgment_with_twist | owner={owner.NetId} | upgraded={twist.CurrentUpgradeLevel}");
            return true;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] acknowledgment_missing_for_replace | owner={owner.NetId} | fallback=add_twist");

        var fallbackTwist = CreateMutable<EnigmaticTheTwist>(owner);
        if (!TryAddCardToRunDeck(owner, fallbackTwist))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_twist_failed | owner={owner.NetId}");
            return false;
        }

        SaveManager.Instance?.MarkCardAsSeen(fallbackTwist.CanonicalInstance ?? fallbackTwist);
        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] added_twist_fallback | owner={owner.NetId} | upgraded={fallbackTwist.CurrentUpgradeLevel}");
        return true;
    }

    public static bool ReplaceTwistWithInfinitum(Player? owner)
    {
        if (owner == null)
            return false;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var twist = deckCards.FirstOrDefault(IsTwistCard);
        if (twist != null)
        {
            if (!EventDeckCardHelper.RemoveCardFromRunDeck(owner, twist))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] remove_twist_failed | owner={owner.NetId}");
                return false;
            }

            var infinitum = CreateMutable<EnigmaticTheInfinitum>(owner, twist);
            if (!TryAddCardToRunDeck(owner, infinitum))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] add_infinitum_failed | owner={owner.NetId}");
                return false;
            }

            SaveManager.Instance?.MarkCardAsSeen(infinitum.CanonicalInstance ?? infinitum);
            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] replaced_twist_with_infinitum | owner={owner.NetId} | upgraded={infinitum.CurrentUpgradeLevel}");
            return true;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] twist_missing_for_replace | owner={owner.NetId} | fallback=add_infinitum");

        var fallbackInfinitum = CreateMutable<EnigmaticTheInfinitum>(owner);
        if (!TryAddCardToRunDeck(owner, fallbackInfinitum))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_infinitum_failed | owner={owner.NetId}");
            return false;
        }

        SaveManager.Instance?.MarkCardAsSeen(fallbackInfinitum.CanonicalInstance ?? fallbackInfinitum);
        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] added_infinitum_fallback | owner={owner.NetId} | upgraded={fallbackInfinitum.CurrentUpgradeLevel}");
        return true;
    }

    public static EnigmaticRevelationKind GetRevelationInHand(Player? owner)
    {
        var handCards = owner?.PlayerCombatState?.Hand?.Cards;
        if (handCards == null)
            return EnigmaticRevelationKind.None;

        if (handCards.Any(IsInfinitumCard))
            return EnigmaticRevelationKind.Infinitum;
        if (handCards.Any(IsTwistCard))
            return EnigmaticRevelationKind.Twist;
        if (handCards.Any(IsAcknowledgmentCard))
            return EnigmaticRevelationKind.Acknowledgment;
        return EnigmaticRevelationKind.None;
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

    public static bool IsTwistCard(CardModel? card)
    {
        if (card == null)
            return false;

        var targetId = ModelDb.Card<EnigmaticTheTwist>().Id;
        return card is EnigmaticTheTwist
               || card.Id == targetId
               || card.CanonicalInstance?.Id == targetId;
    }

    public static bool IsInfinitumCard(CardModel? card)
    {
        if (card == null)
            return false;

        var targetId = ModelDb.Card<EnigmaticTheInfinitum>().Id;
        return card is EnigmaticTheInfinitum
               || card.Id == targetId
               || card.CanonicalInstance?.Id == targetId;
    }

    public static bool IsRevelationCard(CardModel? card)
    {
        return IsAcknowledgmentCard(card) || IsTwistCard(card) || IsInfinitumCard(card);
    }

    private static CardModel CreateMutable<TCard>(Player owner, CardModel? replacedCard = null)
        where TCard : CardModel
    {
        var mutableCard = ModelDb.Card<TCard>().ToMutable();
        mutableCard.Owner = owner;
        mutableCard.FloorAddedToDeck = replacedCard?.FloorAddedToDeck ?? 1;
        CopyUpgradeState(replacedCard, mutableCard);
        return mutableCard;
    }

    private static void CopyUpgradeState(CardModel? source, CardModel mutableCard)
    {
        if (source == null)
            return;

        while (mutableCard.CurrentUpgradeLevel < source.CurrentUpgradeLevel)
        {
            mutableCard.UpgradeInternal();
            mutableCard.FinalizeUpgradeInternal();
        }
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
            EnigmaticOblivionDeckHelper.TryResolveAddedCard(owner, card);
            EtheriumWeaponStrikeReplacementHelper.TryResolveAddedCard(owner, card);
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
        EnigmaticOblivionDeckHelper.TryResolveAddedCard(owner, card);
        EtheriumWeaponStrikeReplacementHelper.TryResolveAddedCard(owner, card);
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
