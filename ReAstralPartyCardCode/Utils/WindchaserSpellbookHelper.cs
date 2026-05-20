using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class WindchaserSpellbookHelper
{
    public static IReadOnlyList<CardModel> CreateUpgradedMutableSpellbookCardsForPlayer(Player owner)
    {
        return WindchaserCompat.GetSpellbookCards()
            .Select(card =>
            {
                var mutable = card.ToMutable();
                mutable.Owner = owner;
                CardCmd.Upgrade(mutable);
                return mutable;
            })
            .ToList();
    }
}
