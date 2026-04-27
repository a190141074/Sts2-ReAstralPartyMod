using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class GeneratedCardObserver
{
    public static async Task AddGeneratedCardToHandAndNotify(CardModel card, bool animate = true,
        CardPilePosition position = CardPilePosition.Top)
    {
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, animate, position);
        await NotifyCardAddedToHand(card);
    }

    public static async Task NotifyCardAddedToHand(CardModel card)
    {
        var recipient = card.Owner;
        if (recipient?.Creature?.CombatState == null)
            return;

        foreach (var player in recipient.Creature.CombatState.Players)
        {
            var relic = player.GetRelic<PersonShadowScion>();
            if (relic == null)
                continue;

            await relic.HandleObservedCardGain(recipient, card);
        }
    }
}
