using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Saves;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SnakebiterDeckReplacementHelper
{
    private static readonly HashSet<string> BaseStrikeEntries =
    [
        "STRIKE_DEFECT",
        "STRIKE_IRONCLAD",
        "STRIKE_NECROBINDER",
        "STRIKE_REGENT",
        "STRIKE_SILENT"
    ];

    private static readonly HashSet<string> BaseDefendEntries =
    [
        "DEFEND_DEFECT",
        "DEFEND_IRONCLAD",
        "DEFEND_NECROBINDER",
        "DEFEND_REGENT",
        "DEFEND_SILENT"
    ];

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
            var replacementId = GetReplacementCardId(card);
            if (replacementId == ModelId.none)
                continue;

            TryReplaceDeckCard(owner, card, replacementId);
        }
    }

    public static bool IsBaseStrike(CardModel? card)
    {
        var entry = (card?.CanonicalInstance ?? card)?.Id.Entry ?? string.Empty;
        return BaseStrikeEntries.Contains(entry);
    }

    public static bool IsBaseDefend(CardModel? card)
    {
        var entry = (card?.CanonicalInstance ?? card)?.Id.Entry ?? string.Empty;
        return BaseDefendEntries.Contains(entry);
    }

    private static ModelId GetReplacementCardId(CardModel? card)
    {
        if (card == null)
            return ModelId.none;
        if (IsBaseStrike(card))
            return ModelDb.GetId<PoisonedStab>();
        if (IsBaseDefend(card))
            return ModelId.none;

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
