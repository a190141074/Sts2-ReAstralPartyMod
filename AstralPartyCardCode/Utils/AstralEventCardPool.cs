using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Relics;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class AstralEventCardPool
{
    private static readonly CardModel[] EventCards =
    [
        ModelDb.Card<EventAngelsDescent>(),
        ModelDb.Card<EventCrowdedPassage>(),
        ModelDb.Card<EventDeusExMachina>(),
        ModelDb.Card<EventEquality>(),
        ModelDb.Card<EventFightFun>(),
        ModelDb.Card<EventFoodSafety>(),
        ModelDb.Card<EventGiftFromSky>(),
        ModelDb.Card<EventHandErase>(),
        ModelDb.Card<EventPlayerRepresentative>(),
        ModelDb.Card<EventRedHeatWarning>(),
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

    public static List<CardModel> CreateMutableEventCardsForPlayer(Player owner, params Type[] excludedTypes)
    {
        HashSet<Type> excludedTypeSet = excludedTypes.Length == 0 ? [] : [..excludedTypes];

        return EventCards
            .Where(card => !excludedTypeSet.Contains(card.GetType()))
            .Select(card =>
            {
                var mutableCard = card.ToMutable();
                mutableCard.Owner = owner;
                return mutableCard;
            })
            .ToList();
    }

    public static List<CardModel> CreateRandomMutableEventCardsForPlayer(Player owner, int count,
        params Type[] excludedTypes)
    {
        return CreateMutableEventCardsForPlayer(owner, excludedTypes)
            .OrderBy(_ => owner.RunState.Rng.Niche.NextInt(int.MaxValue))
            .Take(count)
            .ToList();
    }

    public static List<CardModel> CreateTroubleMakerCardsForPlayer(Player owner, int count)
    {
        var cards = CreateMutableEventCardsForPlayer(owner);

        if (owner.GetRelic<PersonPoisonedApple>() != null)
            cards.AddRange(CreateMutableInvestigationCardsForPlayer(owner));

        return cards
            .OrderBy(_ => owner.RunState.Rng.Niche.NextInt(int.MaxValue))
            .Take(count)
            .ToList();
    }

    public static List<CardModel> CreateMutableInvestigationCardsForPlayer(Player owner)
    {
        return InvestigationCards
            .Select(card =>
            {
                var mutableCard = card.ToMutable();
                mutableCard.Owner = owner;
                return mutableCard;
            })
            .ToList();
    }
}