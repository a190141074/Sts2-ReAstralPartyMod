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

    public static List<CardModel> CreateEventCards(params Type[] excludedTypes)
    {
        HashSet<Type> excludedTypeSet = excludedTypes.Length == 0 ? [] : [.. excludedTypes];

        return AnomalyEventCards
            .Where(card => !excludedTypeSet.Contains(card.GetType()))
            .ToList();
    }

    public static List<CardModel> CreateStableAnomalyMakerCardsForPlayer(Player owner, CardModel sourceCard, int count)
    {
        return AstralStableRandom.PickDistinct(
            CreateEventCards(),
            count,
            static card => card.Id.Entry,
            owner.RunState,
            MainFile.ModId,
            "anomaly_event_pool",
            "anomaly_maker",
            AstralStableRandom.PlayerKey(owner),
            sourceCard.Id.Entry,
            owner.Creature?.CombatState?.RoundNumber ?? 0,
            GetCombatPileSnapshot(owner));
    }

    public static bool IsAnomalyEventCard(CardModel? card)
    {
        if (card == null)
            return false;

        var id = card.CanonicalInstance?.Id ?? card.Id;
        return AnomalyEventCardIds.Contains(id);
    }

    private static int GetCombatPileCount(Player owner, PileType pileType)
    {
        if (owner.Creature?.CombatState == null)
            return 0;

        return pileType.GetPile(owner).Cards.Count;
    }

    private static string GetCombatPileSnapshot(Player owner)
    {
        return string.Join(
            "|",
            GetCombatPileCount(owner, PileType.Draw),
            GetCombatPileCount(owner, PileType.Hand),
            GetCombatPileCount(owner, PileType.Discard),
            GetCombatPileCount(owner, PileType.Exhaust));
    }
}
