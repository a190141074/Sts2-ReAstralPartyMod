using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class UltimateSkillChargeHelper
{
    public const int MaxCharge = 100;

    public static Task HandleAfterCardPlayed(CardPlay cardPlay)
    {
        var owner = cardPlay.Card.Owner;
        if (owner == null || cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;
        if (AttackCardCostHelper.GetPlayedCost(cardPlay) < 1)
            return Task.CompletedTask;

        foreach (var card in EnumerateOwnedUltimateCards(owner))
            card.AddCharge(1);

        return Task.CompletedTask;
    }

    public static void RefreshUltimateCards(Player? player)
    {
        if (player == null)
            return;

        foreach (var card in EnumerateOwnedUltimateCards(player))
            card.RefreshUltimateChargeDisplay();
    }

    private static IEnumerable<UltimateSkillCardModel> EnumerateOwnedUltimateCards(Player player)
    {
        var seenCards = new HashSet<UltimateSkillCardModel>();

        foreach (var card in EventDeckCardHelper.GetRunDeckCards(player).OfType<UltimateSkillCardModel>())
            if (seenCards.Add(card))
                yield return card;

        foreach (var pile in player.Piles)
            foreach (var card in pile.Cards)
                if (card is UltimateSkillCardModel ultimateCard && seenCards.Add(ultimateCard))
                    yield return ultimateCard;
    }
}
