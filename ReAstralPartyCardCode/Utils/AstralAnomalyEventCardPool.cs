using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralAnomalyEventCardPool
{
    private static readonly CardModel[] AnomalyEventCards =
    [
        ModelDb.Card<EventAnomalyBigLuck>(),
        ModelDb.Card<EventAnomalyBugAgain>(),
        ModelDb.Card<EventAnomalyMysticDodge>(),
        ModelDb.Card<EventAnomalyRichCat>(),
        ModelDb.Card<EventAnomalyItsOver>(),
        ModelDb.Card<EventAnomalyBossIsSuperman>(),
        ModelDb.Card<EventAnomalyTransferRequest>(),
        ModelDb.Card<EventAnomalyNoRetreatShrimp>(),
        ModelDb.Card<EventAnomalyLoveCatTv>(),
        ModelDb.Card<EventAnomalyBrokenLeg>(),
        ModelDb.Card<EventAnomalyJustAMob>(),
        ModelDb.Card<EventAnomalyDemonLordProtection>(),
        ModelDb.Card<EventAnomalyBossBurnedOut>()
    ];

    private static readonly HashSet<ModelId> AnomalyEventCardIds =
    [
        .. AnomalyEventCards.Select(card => card.CanonicalInstance?.Id ?? card.Id)
    ];

    public static List<CardModel> CreateMutableEventCardsForPlayer(Player owner, params Type[] excludedTypes)
    {
        HashSet<Type> excludedTypeSet = excludedTypes.Length == 0 ? [] : [.. excludedTypes];

        return AnomalyEventCards
            .Where(card => !excludedTypeSet.Contains(card.GetType()))
            .Select(card =>
            {
                var mutableCard = card.ToMutable();
                mutableCard.Owner = owner;
                return mutableCard;
            })
            .ToList();
    }

    public static List<CardModel> CreateStableAnomalyMakerCardsForPlayer(Player owner, CardModel sourceCard, int count)
    {
        return CreateMutableEventCardsForPlayer(owner)
            .OrderBy(card => GetAnomalyMakerSortKey(owner, sourceCard, card))
            .ThenBy(card => card.Id.Entry, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }

    public static bool IsAnomalyEventCard(CardModel? card)
    {
        if (card == null)
            return false;

        var id = card.CanonicalInstance?.Id ?? card.Id;
        return AnomalyEventCardIds.Contains(id);
    }

    private static uint GetAnomalyMakerSortKey(Player owner, CardModel sourceCard, CardModel candidate)
    {
        var key = string.Join(
            "|",
            owner.RunState.Rng.StringSeed,
            owner.NetId,
            owner.Creature?.CombatState?.RoundNumber ?? 0,
            sourceCard.Id.Entry,
            GetCombatPileCount(owner, PileType.Draw),
            GetCombatPileCount(owner, PileType.Hand),
            GetCombatPileCount(owner, PileType.Discard),
            GetCombatPileCount(owner, PileType.Exhaust),
            candidate.Id.Entry);

        return (uint)StringHelper.GetDeterministicHashCode(key);
    }

    private static int GetCombatPileCount(Player owner, PileType pileType)
    {
        if (owner.Creature?.CombatState == null)
            return 0;

        return pileType.GetPile(owner).Cards.Count;
    }
}
