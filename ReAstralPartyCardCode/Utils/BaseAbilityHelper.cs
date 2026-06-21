using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class BaseAbilityHelper
{
    public static bool HasOtherLivingPlayerTarget(Player? owner)
    {
        var combatState = owner?.Creature?.CombatState;
        if (owner == null || combatState == null)
            return false;

        return combatState.PlayerCreatures.Any(creature => creature.IsAlive && creature.Player != owner);
    }

    public static bool IsOtherLivingPlayerTarget(Player? owner, Creature? target)
    {
        var targetPlayer = target?.Player;
        if (owner == null || target == null || targetPlayer == null)
            return false;

        return target.IsAlive && targetPlayer != owner;
    }

    public static bool IsCardTypeAvailableForPlayer(Type type, Player? owner)
    {
        if (!typeof(CardModel).IsAssignableFrom(type))
            return false;

        var card = ModelDb.GetById<CardModel>(ModelDb.GetId(type));
        return IsCardAvailableForPlayer(card, owner);
    }

    public static bool IsCardAvailableForPlayer(CardModel? card, Player? owner)
    {
        if (card == null)
            return false;

        if (card.MultiplayerConstraint != CardMultiplayerConstraint.MultiplayerOnly)
            return true;

        return HasOtherLivingPlayerTarget(owner);
    }

    public static async Task<CardModel?> GrantDeterministicBaseAbilityToHand(
        Player owner,
        Player recipient,
        AbstractModel source,
        params object?[] contextParts)
    {
        if (owner.Creature?.CombatState == null)
            return null;

        var selectedCard = BaseAbilityCardRegistry.GetStableRandomCardModel(owner, contextParts);
        if (selectedCard == null)
            return null;

        var createdCard = owner.Creature.CombatState.CreateCard(selectedCard.CanonicalInstance ?? selectedCard, recipient);
        await PersonMultiplayerEffectHelper.MoveOwnedCombatCardToHandAndNotify(
            createdCard,
            CardPilePosition.Top,
            source);
        return createdCard;
    }
}
