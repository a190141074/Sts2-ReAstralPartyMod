using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralEventCardCatalog
{
    private static readonly CardModel[] EventCards =
    [
        ModelDb.Card<EventAngelsDescent>(),
        ModelDb.Card<EventCrowdedPassage>(),
        ModelDb.Card<EventDeusExMachina>(),
        ModelDb.Card<EventEquality>(),
        ModelDb.Card<EventFightFun>(),
        ModelDb.Card<EventFoodSafety>(),
        ModelDb.Card<EventFoodSafetyDoom>(),
        ModelDb.Card<EventGiftFromSky>(),
        ModelDb.Card<EventHandErase>(),
        ModelDb.Card<EventPlayerRepresentative>(),
        ModelDb.Card<EventRedHeatWarning>(),
        ModelDb.Card<EventThunderApproaches>(),
        ModelDb.Card<EventSprint>(),
        ModelDb.Card<EventThunderStrike>()
    ];

    private static readonly CardModel[] InvestigationCards =
    [
        ModelDb.Card<EventsConcealingInvestigationA>(),
        ModelDb.Card<EventsConcealingInvestigationB>(),
        ModelDb.Card<EventsConcealingInvestigationC>(),
        ModelDb.Card<EventsConcealingInvestigationD>()
    ];

    private static readonly HashSet<ModelId> EventCardIds =
    [
        .. EventCards.Select(card => card.CanonicalInstance?.Id ?? card.Id),
        .. InvestigationCards.Select(card => card.CanonicalInstance?.Id ?? card.Id)
    ];

    private static readonly HashSet<ModelId> TroubleMakerUnsafeEventCardIds =
    [
        ModelDb.GetId<EventDeusExMachina>(),
        ModelDb.GetId<EventsConcealingInvestigationA>(),
        ModelDb.GetId<EventsConcealingInvestigationB>(),
        ModelDb.GetId<EventsConcealingInvestigationC>(),
        ModelDb.GetId<EventsConcealingInvestigationD>()
    ];

    public static List<CardModel> CreateEventCards(params Type[] excludedTypes)
    {
        HashSet<Type> excludedTypeSet = excludedTypes.Length == 0 ? [] : [..excludedTypes];

        return EventCards
            .Where(card => !excludedTypeSet.Contains(card.GetType()))
            .ToList();
    }

    public static List<CardModel> CreateRandomEventCardsForPlayer(Player owner, int count,
        params Type[] excludedTypes)
    {
        return CreateEventCards(excludedTypes)
            .OrderBy(card => GetDeterministicEventCardSortKey(owner, card))
            .ThenBy(card => card.Id.Entry, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }

    public static List<CardModel> CreateStableTroubleMakerCardsForPlayer(Player owner, CardModel sourceCard, int count)
    {
        var cards = CreateEventCards();

        if (RelicOwnershipHelper.HasRelic<PersonPoisonedApple>(owner))
            cards.AddRange(CreateInvestigationCards());

        return cards
            .OrderBy(card => GetTroubleMakerSortKey(owner, sourceCard, card))
            .ThenBy(card => card.Id.Entry)
            .Take(count)
            .ToList();
    }

    public static List<CardModel> CreateStableBossBurnedOutSafeCardsForPlayer(Player owner, CardModel sourceCard,
        int count)
    {
        var cards = CreateEventCards()
            .Where(IsBossBurnedOutSafeCard)
            .ToList();

        return cards
            .OrderBy(card => GetTroubleMakerSortKey(owner, sourceCard, card))
            .ThenBy(card => card.Id.Entry, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }

    private static uint GetTroubleMakerSortKey(Player owner, CardModel sourceCard, CardModel candidate)
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

    private static uint GetDeterministicEventCardSortKey(Player owner, CardModel candidate)
    {
        var key = string.Join(
            "|",
            owner.RunState.Rng.StringSeed,
            owner.NetId,
            owner.RunState.CurrentActIndex,
            owner.RunState.ActFloor,
            candidate.Id.Entry);

        return (uint)StringHelper.GetDeterministicHashCode(key);
    }

    private static int GetCombatPileCount(Player owner, PileType pileType)
    {
        if (owner.Creature?.CombatState == null)
            return 0;

        return pileType.GetPile(owner).Cards.Count;
    }

    public static List<CardModel> CreateInvestigationCards()
    {
        return InvestigationCards
            .ToList();
    }

    public static bool IsEventCard(CardModel? card)
    {
        if (card == null)
            return false;

        var id = card.CanonicalInstance?.Id ?? card.Id;
        return EventCardIds.Contains(id);
    }

    private static bool IsBossBurnedOutSafeCard(CardModel card)
    {
        var id = card.CanonicalInstance?.Id ?? card.Id;
        return !TroubleMakerUnsafeEventCardIds.Contains(id);
    }
}
