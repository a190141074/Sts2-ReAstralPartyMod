using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class DefendPointCardQualificationHelper
{
    private static readonly HashSet<string> DelayedBlockQualifiedCardIds = new(StringComparer.Ordinal);

    public static bool CountsAsDefendPointCard(CardModel? card)
    {
        if (card == null)
            return false;

        return card.GainsBlock
               || GainsBlockFromCompatOrEnchantment(card)
               || DelayedBlockQualifiedCardIds.Contains((card.CanonicalInstance?.Id ?? card.Id).Entry);
    }

    private static bool GainsBlockFromCompatOrEnchantment(CardModel card)
    {
        if (TetraWarforgeEnchantmentHelper.HasWarforge(card) && card.Type == CardType.Attack)
            return true;

        return false;
    }
}
