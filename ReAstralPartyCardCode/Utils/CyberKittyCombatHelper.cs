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
    private const decimal StandardNodeDiscountAmount = 1m;
    private const decimal HighCostNodeDiscountAmount = 2m;
    private const int HighCostThreshold = 3;

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

    public static async Task<CardModel?> GainRandomAttackCardDiscounted(
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

        var discountAmount = GetNodeDiscountAmount(attackCard);
        if (discountAmount <= 0m)
            return attackCard;

        var discountPower = owner.Creature.GetPower<CyberKittyDiscountedAttackPower>()
                            ?? await CreateDiscountPower(owner, source as CardModel);
        discountPower?.TrackCard(attackCard, discountAmount);
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

    private static decimal GetNodeDiscountAmount(CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return 0m;

        var resolvedCost = card.EnergyCost.GetResolved();
        return resolvedCost > HighCostThreshold
            ? HighCostNodeDiscountAmount
            : StandardNodeDiscountAmount;
    }

    private static async Task<CyberKittyDiscountedAttackPower?> CreateDiscountPower(Player owner, CardModel? source)
    {
        var power = ModelDb.Power<CyberKittyDiscountedAttackPower>().ToMutable() as CyberKittyDiscountedAttackPower;
        if (power == null)
            return null;

        await PowerCmd.Apply(power, owner.Creature, 1m, owner.Creature, source, false);
        return owner.Creature.GetPower<CyberKittyDiscountedAttackPower>();
    }
}
