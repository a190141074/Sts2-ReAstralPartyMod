using System.Text.Json;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EnigmaticOblivionDeckHelper
{
    public static string SerializeIdSet(IEnumerable<string> ids)
    {
        return JsonSerializer.Serialize(ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray());
    }

    public static HashSet<string> DeserializeIdSet(string value)
    {
        try
        {
            var ids = JsonSerializer.Deserialize<string[]>(value) ?? [];
            return ids
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }

    public static bool ShouldObliviate(Player? owner, CardModel? card)
    {
        if (owner == null || card == null)
            return false;

        return owner.GetRelic<EnigmaticVoidStone>()?.ContainsObliviatedCard(card) == true;
    }

    public static bool TryResolveAddedCard(Player owner, CardModel? addedCard)
    {
        if (!ShouldObliviate(owner, addedCard) || addedCard == null)
            return false;

        return EventDeckCardHelper.RemoveCardFromRunDeck(owner, addedCard);
    }

    public static string GetCanonicalCardEntry(CardModel? card)
    {
        return (card?.CanonicalInstance ?? card)?.Id.Entry ?? string.Empty;
    }

    public static CardModel? FindMatchingDeckCard(Player owner, CardModel? selectedCanonicalCard)
    {
        if (selectedCanonicalCard == null)
            return null;

        var targetEntry = GetCanonicalCardEntry(selectedCanonicalCard);
        if (string.IsNullOrWhiteSpace(targetEntry))
            return null;

        return EventDeckCardHelper.GetRunDeckCards(owner)
            .FirstOrDefault(card => string.Equals(
                GetCanonicalCardEntry(card),
                targetEntry,
                StringComparison.Ordinal));
    }
}
