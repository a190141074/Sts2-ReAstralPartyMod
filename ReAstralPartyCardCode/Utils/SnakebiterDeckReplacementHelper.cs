using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.TestSupport;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SnakebiterDeckReplacementHelper
{
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

    public static void ReplaceCurrentDeck(Player? owner)
    {
        if (owner == null)
            return;

        var cards = EventDeckCardHelper.GetRunDeckCards(owner).ToList();
        foreach (var card in cards)
        {
            var replacementId = GetReplacementCardId(owner, card);
            if (replacementId == ModelId.none)
                continue;

            TryReplaceDeckCard(owner, card, replacementId);
        }
    }

    public static bool IsBaseStrike(Player? owner, CardModel? card)
    {
        return card != null && GetCanonicalCardId(card) == GetBaseStrikeId(owner);
    }

    public static bool IsBaseDefend(Player? owner, CardModel? card)
    {
        return card != null && GetCanonicalCardId(card) == GetBaseDefendId(owner);
    }

    private static ModelId GetReplacementCardId(Player? owner, CardModel? card)
    {
        if (card == null)
            return ModelId.none;
        if (IsBaseStrike(owner, card))
            return ModelDb.GetId<PoisonedStab>();
        if (IsBaseDefend(owner, card))
            return ModelDb.GetId<DodgeAndRoll>();

        return ModelDb.GetId<Snakebite>();
    }

    private static bool TryReplaceDeckCard(Player owner, CardModel sourceCard, ModelId replacementId)
    {
        if ((sourceCard.CanonicalInstance?.Id ?? sourceCard.Id) == replacementId)
            return false;

        var replacement = ModelDb.GetById<CardModel>(replacementId).ToMutable();
        replacement.Owner = owner;
        replacement.FloorAddedToDeck = sourceCard.FloorAddedToDeck;
        CopyUpgradeState(sourceCard, replacement);

        var replaced = ReplaceRunStateCard(owner, sourceCard, replacement)
                       | ReplacePlayerDeckCard(owner, sourceCard, replacement);
        if (!replaced)
            return false;

        SaveManager.Instance?.MarkCardAsSeen(replacement.CanonicalInstance ?? replacement);
        MainFile.Logger.Info(
            $"[SnakebiterDeckReplacementHelper] replaced_card | owner={owner.NetId} | from={sourceCard.Id.Entry} | to={replacement.Id.Entry} | upgraded={replacement.CurrentUpgradeLevel}");
        return true;
    }

    private static bool ReplaceRunStateCard(Player owner, CardModel sourceCard, CardModel replacement)
    {
        var runState = owner.RunState;
        if (runState == null)
            return false;

        foreach (var memberName in RunDeckMemberNames)
        {
            var member = runState.GetType().GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(runState, member) is not IList list)
                continue;

            var index = list.IndexOf(sourceCard);
            if (index < 0)
                continue;

            list[index] = replacement;
            return true;
        }

        return false;
    }

    private static bool ReplacePlayerDeckCard(Player owner, CardModel sourceCard, CardModel replacement)
    {
        var deck = owner.Deck;
        if (deck == null)
            return false;

        if (deck.Cards is IList deckCards)
        {
            var index = deckCards.IndexOf(sourceCard);
            if (index >= 0)
            {
                deckCards[index] = replacement;
                return true;
            }
        }

        foreach (var memberName in CardCollectionMemberNames)
        {
            var member = typeof(CardPile)
                .GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault();
            if (member == null)
                continue;

            if (ReadMemberValue(deck, member) is not IList list)
                continue;

            var index = list.IndexOf(sourceCard);
            if (index < 0)
                continue;

            list[index] = replacement;
            return true;
        }

        return false;
    }

    private static void CopyUpgradeState(CardModel source, CardModel replacement)
    {
        while (replacement.CurrentUpgradeLevel < source.CurrentUpgradeLevel)
        {
            replacement.UpgradeInternal();
            replacement.FinalizeUpgradeInternal();
        }
    }

    private static ModelId GetBaseStrikeId(Player? owner)
    {
        var character = owner?.Character;
        if (character == null)
            return ModelId.none;
        if (TestMode.IsOn && character is Deprived)
            return ModelDb.GetId<StrikeIronclad>();

        var baseStrike = character.CardPool.AllCards.FirstOrDefault(static card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Strike));
        return baseStrike?.Id ?? ModelId.none;
    }

    private static ModelId GetBaseDefendId(Player? owner)
    {
        var character = owner?.Character;
        if (character == null)
            return ModelId.none;
        if (TestMode.IsOn && character is Deprived)
            return ModelDb.GetId<DefendIronclad>();

        var baseDefend = character.CardPool.AllCards.FirstOrDefault(static card =>
            card.Rarity == CardRarity.Basic && card.Tags.Contains(CardTag.Defend));
        return baseDefend?.Id ?? ModelId.none;
    }

    private static ModelId GetCanonicalCardId(CardModel card)
    {
        return card.CanonicalInstance?.Id ?? card.Id;
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
