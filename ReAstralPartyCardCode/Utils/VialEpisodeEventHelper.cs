using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class VialEpisodeEventHelper
{
    public static IReadOnlyList<CardModel> CreateNeutralEventOptions(Player owner)
    {
        return CreateCanonicalOptions(
            owner,
            ModelDb.Card<EventCrowdedPassage>(),
            ModelDb.Card<EventEquality>(),
            ModelDb.Card<EventPlayerRepresentative>(),
            ModelDb.Card<EventThunderApproaches>());
    }

    public static IReadOnlyList<CardModel> CreateGoodEventOptions(Player owner)
    {
        return CreateCanonicalOptions(
            owner,
            ModelDb.Card<EventFightFun>(),
            ModelDb.Card<EventGiftFromSky>(),
            ModelDb.Card<EventRedHeatWarning>(),
            ModelDb.Card<EventSprint>());
    }

    public static async Task AutoPlayCanonicalCardForOwner(
        Player owner,
        CardModel canonicalCard)
    {
        if (owner.Creature?.CombatState == null)
            return;

        var cardToPlay = owner.Creature.CombatState.CreateCard(canonicalCard.CanonicalInstance ?? canonicalCard, owner);
        await CardCmd.AutoPlay(new ThrowingPlayerChoiceContext(), cardToPlay, owner.Creature, AutoPlayType.Default, false, true);
    }

    private static IReadOnlyList<CardModel> CreateCanonicalOptions(Player owner, params CardModel[] cards)
    {
        return cards
            .Select(card =>
            {
                var mutable = card.ToMutable();
                mutable.Owner = owner;
                return mutable.CanonicalInstance ?? mutable;
            })
            .OrderBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
}
