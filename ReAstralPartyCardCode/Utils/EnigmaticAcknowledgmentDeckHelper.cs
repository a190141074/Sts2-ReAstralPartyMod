using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
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

    public static async Task<bool> EnsureInRunDeck(Player owner)
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
        if (ascendersBane != null && !await EventDeckCardHelper.RemoveCardFromRunDeck(owner, ascendersBane))
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
        if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, mutableCard))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_failed | owner={owner.NetId} | card={mutableCard.Id.Entry}");
            return false;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] added_acknowledgment | owner={owner.NetId} | replacedAscendersBane={ascendersBane != null}");
        return true;
    }

    public static async Task<bool> ReplaceAcknowledgmentWithTwist(Player? owner)
    {
        if (owner == null)
            return false;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var acknowledgment = deckCards.FirstOrDefault(IsAcknowledgmentCard);
        if (acknowledgment != null)
        {
            if (!await EventDeckCardHelper.RemoveCardFromRunDeck(owner, acknowledgment))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] remove_acknowledgment_failed | owner={owner.NetId}");
                return false;
            }

            var twist = CreateMutable<EnigmaticTheTwist>(owner, acknowledgment);
            if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, twist))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] add_twist_failed | owner={owner.NetId}");
                return false;
            }

            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] replaced_acknowledgment_with_twist | owner={owner.NetId} | upgraded={twist.CurrentUpgradeLevel}");
            return true;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] acknowledgment_missing_for_replace | owner={owner.NetId} | fallback=add_twist");

        var fallbackTwist = CreateMutable<EnigmaticTheTwist>(owner);
        if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, fallbackTwist))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_twist_failed | owner={owner.NetId}");
            return false;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] added_twist_fallback | owner={owner.NetId} | upgraded={fallbackTwist.CurrentUpgradeLevel}");
        return true;
    }

    public static async Task<bool> ReplaceTwistWithInfinitum(Player? owner)
    {
        if (owner == null)
            return false;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        var twist = deckCards.FirstOrDefault(IsTwistCard);
        if (twist != null)
        {
            if (!await EventDeckCardHelper.RemoveCardFromRunDeck(owner, twist))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] remove_twist_failed | owner={owner.NetId}");
                return false;
            }

            var infinitum = CreateMutable<EnigmaticTheInfinitum>(owner, twist);
            if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, infinitum))
            {
                MainFile.Logger.Warn(
                    $"[EnigmaticAcknowledgmentDeckHelper] add_infinitum_failed | owner={owner.NetId}");
                return false;
            }

            MainFile.Logger.Info(
                $"[EnigmaticAcknowledgmentDeckHelper] replaced_twist_with_infinitum | owner={owner.NetId} | upgraded={infinitum.CurrentUpgradeLevel}");
            return true;
        }

        MainFile.Logger.Info(
            $"[EnigmaticAcknowledgmentDeckHelper] twist_missing_for_replace | owner={owner.NetId} | fallback=add_infinitum");

        var fallbackInfinitum = CreateMutable<EnigmaticTheInfinitum>(owner);
        if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, fallbackInfinitum))
        {
            MainFile.Logger.Warn(
                $"[EnigmaticAcknowledgmentDeckHelper] add_infinitum_failed | owner={owner.NetId}");
            return false;
        }

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

}
