using AstralPartyMod.AstralPartyCardCode.cards;
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
        ModelDb.Card<EventsConcealingInvestigationA>(),
        ModelDb.Card<EventsConcealingInvestigationB>(),
        ModelDb.Card<EventsConcealingInvestigationC>(),
        ModelDb.Card<EventsConcealingInvestigationD>(),
        ModelDb.Card<EventRedHeatWarning>(),
        ModelDb.Card<EventSprint>(),
        ModelDb.Card<EventThunderStrike>()
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
}
