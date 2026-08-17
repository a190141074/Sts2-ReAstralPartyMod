using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class FallenHeavenlyKingDeckHelper
{
    public static async Task DuplicateDeckTwice(Player owner)
    {
        var runState = owner.RunState
                       ?? throw new InvalidOperationException(
                           $"Cannot duplicate the deck for player {owner.NetId}: no run state is available.");
        var originals = EventDeckCardHelper.GetRunDeckCards(owner)
            .Select(card => (CardId: card.Id.Entry, Snapshot: card.ToSerializable()))
            .ToList();
        foreach (var source in originals)
        {
            for (var copyIndex = 0; copyIndex < 2; copyIndex++)
            {
                try
                {
                    var copy = runState.LoadCard(source.Snapshot, owner);
                    if (!await EventDeckCardHelper.AddCardToRunDeckAsync(owner, copy, true))
                    {
                        throw new InvalidOperationException(
                            $"Could not add duplicated card '{source.CardId}' to player {owner.NetId}'s deck.");
                    }
                }
                catch (Exception exception)
                {
                    MainFile.Logger.Error(
                        $"[FallenHeavenlyKing] deck duplication failed | owner={owner.NetId} | card={source.CardId} | copy={copyIndex + 1}: {exception}");
                    throw;
                }
            }
        }

        MainFile.Logger.Info(
            $"[FallenHeavenlyKing] duplicated deck twice | owner={owner.NetId} | originals={originals.Count}");
    }

    public static async Task ReplaceWithHeavenlyKingDeck(Player owner)
    {
        foreach (var card in EventDeckCardHelper.GetRunDeckCards(owner).ToList())
            await EventDeckCardHelper.RemoveCardFromRunDeck(owner, card, false);

        await AddCopies<HkMajestyOfTheHeavenlyKing>(owner, 3);
        await AddCopies<HkExclusiveChannel>(owner, 3);
        await AddCopies<HkHeavenlyKingsPresence>(owner, 3);
        await AddCopies<HkFallOfTheHeavenlyKing>(owner, 1);
        MainFile.Logger.Info($"[FallenHeavenlyKing] replaced deck | owner={owner.NetId} | cards=10");
    }

    public static void ReduceFallCost(Player owner, int amount)
    {
        if (amount <= 0)
            return;

        var deckCard = EventDeckCardHelper.GetRunDeckCards(owner).OfType<HkFallOfTheHeavenlyKing>().FirstOrDefault();
        if (deckCard == null)
            return;

        var nextCost = Math.Max(0, deckCard.EnergyCost.GetWithModifiers(CostModifiers.None) - amount);
        deckCard.EnergyCost.SetCustomBaseCost(nextCost);

        foreach (var combatCard in PileType.Hand.GetPile(owner).Cards.OfType<HkFallOfTheHeavenlyKing>())
            combatCard.EnergyCost.SetCustomBaseCost(nextCost);
    }

    private static async Task AddCopies<TCard>(Player owner, int count) where TCard : CardModel
    {
        for (var index = 0; index < count; index++)
        {
            var card = ModelDb.Card<TCard>().ToMutable();
            card.FloorAddedToDeck = Math.Max(owner.RunState?.TotalFloor ?? 1, 1);
            await EventDeckCardHelper.AddCardToRunDeckAsync(owner, card, false);
        }
    }
}
