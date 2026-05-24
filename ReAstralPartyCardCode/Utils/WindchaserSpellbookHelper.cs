using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class WindchaserSpellbookHelper
{
    public static IReadOnlyList<CardModel> CreateUpgradedSpellbookCardsForPlayer(Player owner)
    {
        return WindchaserCompat.GetSpellbookCards()
            .Select(card =>
            {
                var upgradedCard = card.ToMutable();
                upgradedCard.Owner = owner;
                CardCmd.Upgrade(upgradedCard);
                return upgradedCard;
            })
            .ToList();
    }
}
