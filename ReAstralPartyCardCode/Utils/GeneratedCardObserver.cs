using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
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
        CombatEntryDiagnostics.Mark("generated_card_observer", card.Owner);
        await CardGainAttribution.RunWithSource(source, async () =>
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, card.Owner, position);
            await NotifyCardAddedToHand(card, source);
        });
    }

    public static Task NotifyCardAddedToHand(CardModel card, AbstractModel? source = null)
    {
        return NotifyCardGained(card, source);
    }

    public static Task NotifyCardGained(CardModel card, AbstractModel? source = null)
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
                var players = recipient.Creature.CombatState.Players
                    .OrderBy(player => player.NetId)
                    .ToList();
                var vitamin = recipient.GetRelic<TokenGoldVitamin>();
                if (vitamin != null)
                    await vitamin.HandleGeneratedCard(card);

                foreach (var player in players)
                {
                    var relic = player.GetRelic<PersonShadowScion>();
                    if (relic != null)
                        await relic.HandleObservedCardGain(recipient, source);

                    var variantRelic = player.GetRelic<VariantPersonShadowScion>();
                    if (variantRelic != null)
                        await variantRelic.HandleObservedCardGain(recipient, source);

                    var lakeMarshKing = player.GetRelic<PersonalityDerivativeLakeMarshKing>();
                    if (lakeMarshKing != null)
                        await lakeMarshKing.HandleObservedCardGain(recipient, source);
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
