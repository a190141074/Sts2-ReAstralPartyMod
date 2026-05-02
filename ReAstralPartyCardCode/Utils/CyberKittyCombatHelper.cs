using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CyberKittyCombatHelper
{
    public static async Task<CardModel?> GainRandomAttackCardFreeThisTurn(
        PlayerChoiceContext choiceContext,
        Player owner,
        AbstractModel source)
    {
        if (owner.Creature?.CombatState == null)
            return null;

        var attackCard = FindPreferredAttackCard(owner);
        if (attackCard == null)
            return null;

        await CardPileCmd.Add(attackCard, PileType.Hand, CardPilePosition.Bottom, source);
        attackCard.SetToFreeThisTurn();
        return attackCard;
    }

    public static async Task<CardModel?> DiscardLeftmostAttackAndUpgradeForCombat(
        PlayerChoiceContext choiceContext,
        Player owner)
    {
        var leftmostAttack = PileType.Hand
            .GetPile(owner)
            .Cards
            .FirstOrDefault(card => card.Type == CardType.Attack);

        if (leftmostAttack == null)
            return null;

        await CardCmd.Discard(choiceContext, leftmostAttack);
        CardCmd.Upgrade(leftmostAttack);
        return leftmostAttack;
    }

    public static CardModel? UpgradeRandomAttackForCombat(Player owner)
    {
        var playerCombatState = owner.PlayerCombatState;
        if (playerCombatState == null)
            return null;

        var selected = PersonaMultiplayerEffectHelper.SelectRandomUpgradeableCombatCard(
            owner,
            card => card.Type == CardType.Attack,
            owner.RunState.Rng.CombatCardSelection);
        var fallback = selected ?? PersonaMultiplayerEffectHelper.SelectRandomUpgradeableCombatCard(
            owner,
            _ => true,
            owner.RunState.Rng.CombatCardSelection);
        if (fallback == null)
            return null;

        CardCmd.Upgrade(fallback);
        return fallback;
    }

    private static CardModel? FindPreferredAttackCard(Player owner)
    {
        var drawAttack = PileType.Draw
            .GetPile(owner)
            .Cards
            .FirstOrDefault(card => card.Type == CardType.Attack);
        if (drawAttack != null)
            return drawAttack;

        return PileType.Discard
            .GetPile(owner)
            .Cards
            .FirstOrDefault(card => card.Type == CardType.Attack);
    }
}
