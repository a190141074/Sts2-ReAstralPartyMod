using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class VialEpisodeEventHelper
{
    public static IReadOnlyList<CardModel> CreateNeutralEventOptions(Player owner, string context)
    {
        return CreateRandomChoiceOptions(
            owner,
            context,
            typeof(EventCrowdedPassage),
            typeof(EventEquality),
            typeof(EventPlayerRepresentative),
            typeof(EventThunderApproaches));
    }

    public static IReadOnlyList<CardModel> CreateGoodEventOptions(Player owner, string context)
    {
        return CreateRandomChoiceOptions(
            owner,
            context,
            typeof(EventFightFun),
            typeof(EventGiftFromSky),
            typeof(EventRedHeatWarning),
            typeof(EventSprint));
    }

    public static async Task PlaySelectedCanonicalCardForOwner(
        PlayerChoiceContext choiceContext,
        Player owner,
        IReadOnlyList<CardModel> options,
        string context)
    {
        var selected = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            owner,
            options,
            false,
            context);
        if (selected == null)
            return;

        await AutoPlayCanonicalCardForOwner(choiceContext, owner, selected);
    }

    public static async Task AutoPlayCanonicalCardForOwner(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardModel canonicalCard)
    {
        if (owner.Creature?.CombatState == null)
            return;

        var cardToPlay = owner.Creature.CombatState.CreateCard(canonicalCard.CanonicalInstance ?? canonicalCard, owner);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, owner.Creature, AutoPlayType.Default, false, true);
    }

    private static IReadOnlyList<CardModel> CreateRandomChoiceOptions(Player owner, string context,
        params Type[] cardTypes)
    {
        var orderedCanonicals = DeterministicMultiplayerChoiceHelper
            .OrderDeterministically(
                cardTypes.Select(type => ModelDb.GetById<CardModel>(ModelDb.GetId(type))),
                card => card.Id.Entry,
                MainFile.ModId,
                context,
                owner.RunState.Rng.StringSeed,
                owner.RunState.CurrentActIndex,
                owner.RunState.ActFloor,
                owner.NetId)
            .Take(3);

        return orderedCanonicals
            .Select(card =>
            {
                var mutable = card.ToMutable();
                mutable.Owner = owner;
                return mutable;
            })
            .ToList();
    }
}
