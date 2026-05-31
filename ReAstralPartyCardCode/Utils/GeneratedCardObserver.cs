using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class GeneratedCardObserver
{
    private static readonly HashSet<CardModel> CardsBeingNotified = [];
    private static readonly HashSet<CardModel> CardsAlreadyNotified = [];
    private static bool _cleanupHooksRegistered;

    public static void EnsureCleanupHooksRegistered()
    {
        if (_cleanupHooksRegistered)
            return;

        _cleanupHooksRegistered = true;
        CombatManager.Instance.CombatEnded += _ => Clear();
        RunManager.Instance.RoomEntered += ClearIfOutOfCombat;
    }

    public static void Clear()
    {
        CardsBeingNotified.Clear();
        CardsAlreadyNotified.Clear();
    }

    public static async Task AddGeneratedCardToHandAndNotify(CardModel card, bool animate = true,
        CardPilePosition position = CardPilePosition.Top, AbstractModel? source = null)
    {
        EnsureCleanupHooksRegistered();
        await CardGainAttribution.RunWithSource(source, async () =>
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, animate, position);
            await NotifyCardAddedToHand(card, source);
        });
    }

    public static Task NotifyCardAddedToHand(CardModel card, AbstractModel? source = null)
    {
        EnsureCleanupHooksRegistered();
        return CardGainAttribution.RunWithSource(source, async () =>
        {
            var recipient = card.Owner;
            if (recipient?.Creature?.CombatState == null)
                return;
            if (!CardsAlreadyNotified.Add(card))
                return;
            if (!CardsBeingNotified.Add(card))
                return;

            try
            {
                foreach (var player in recipient.Creature.CombatState.Players)
                {
                    var relic = player.GetRelic<PersonShadowScion>();
                    if (relic == null)
                        continue;

                    await relic.HandleObservedCardGain(recipient, source);
                }
            }
            finally
            {
                CardsBeingNotified.Remove(card);
            }
        });
    }

    private static void ClearIfOutOfCombat()
    {
        if (!CombatManager.Instance.IsInProgress)
            Clear();
    }
}
