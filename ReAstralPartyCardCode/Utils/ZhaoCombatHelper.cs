using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ZhaoCombatHelper
{
    public static async Task<CardModel?> AutoPlayRandomAttackForZhao(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature target,
        decimal bonusDamage,
        AbstractModel source)
    {
        if (owner.Creature?.CombatState == null || !target.IsAlive)
            return null;

        var attackCard = FindDeterministicAttackCard(owner, target, bonusDamage, source);
        if (attackCard == null)
            return null;

        var extraAttackPower = owner.Creature.GetPower<ExtraAttackPower>();
        if (extraAttackPower == null)
            return null;

        extraAttackPower.BeginTriggeredAttack(attackCard, target, bonusDamage);
        try
        {
            await CardCmd.AutoPlay(choiceContext, attackCard, target, AutoPlayType.Default, false, true);
            if (ShouldTriggerNecrobinderUnleash(owner, source))
                await AutoPlayNecrobinderUnleash(choiceContext, owner, target, source);
            return attackCard;
        }
        finally
        {
            extraAttackPower.EndTriggeredAttack(attackCard);
        }
    }

    private static CardModel? FindDeterministicAttackCard(
        Player owner,
        Creature target,
        decimal bonusDamage,
        AbstractModel source)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        var drawPileAttacks = PileType.Draw
            .GetPile(owner)
            .Cards
            .Where(WarforgeEnchantmentHelper.CountsAsAttack)
            .ToList();
        if (drawPileAttacks.Count > 0)
            return AstralStableRandom.Pick(
                drawPileAttacks,
                GetStableAttackCardKey,
                owner.RunState,
                MainFile.ModId,
                "zhao_chase",
                source.Id.Entry,
                AstralStableRandom.PlayerKey(owner),
                combatState.RoundNumber,
                bonusDamage,
                PileType.Draw,
                GetPileSnapshot(drawPileAttacks));

        var discardPileAttacks = PileType.Discard
            .GetPile(owner)
            .Cards
            .Where(WarforgeEnchantmentHelper.CountsAsAttack)
            .ToList();
        if (discardPileAttacks.Count > 0)
            return AstralStableRandom.Pick(
                discardPileAttacks,
                GetStableAttackCardKey,
                owner.RunState,
                MainFile.ModId,
                "zhao_chase",
                source.Id.Entry,
                AstralStableRandom.PlayerKey(owner),
                combatState.RoundNumber,
                bonusDamage,
                PileType.Discard,
                GetPileSnapshot(discardPileAttacks));

        return null;
    }

    private static string GetStableAttackCardKey(CardModel card)
    {
        var pile = card.Pile;
        var pileType = pile?.Type.ToString() ?? "None";
        var pileIndex = GetPileIndex(pile?.Cards, card);
        var canonicalId = (card.CanonicalInstance?.Id ?? card.Id).Entry;
        return $"{pileType}:{pileIndex:D4}:{canonicalId}";
    }

    private static int GetPileIndex(IReadOnlyList<CardModel>? cards, CardModel target)
    {
        if (cards == null)
            return -1;

        for (var i = 0; i < cards.Count; i++)
            if (ReferenceEquals(cards[i], target))
                return i;

        return -1;
    }

    private static string GetPileSnapshot(IReadOnlyList<CardModel> cards)
    {
        return string.Join(",", cards.Select(GetStableAttackCardKey));
    }

    private static bool ShouldTriggerNecrobinderUnleash(Player owner, AbstractModel source)
    {
        if (owner.Character.Id.Entry != "NECROBINDER")
            return false;

        return source is PersonZhao;
    }

    private static async Task AutoPlayNecrobinderUnleash(
        PlayerChoiceContext choiceContext,
        Player owner,
        Creature target,
        AbstractModel source)
    {
        if (owner.Creature?.CombatState == null || !target.IsAlive)
            return;
        if (!HasUnleashAvailable(owner))
            return;

        var unleashCard = owner.Creature.CombatState.CreateCard(ModelDb.Card<Unleash>(), owner);
        var extraAttackPower = owner.Creature.GetPower<ExtraAttackPower>();
        if (extraAttackPower == null)
            return;

        extraAttackPower.BeginTriggeredAttack(unleashCard, target, 0m);
        try
        {
            await CardCmd.AutoPlay(choiceContext, unleashCard, target, AutoPlayType.Default, false, true);
        }
        finally
        {
            extraAttackPower.EndTriggeredAttack(unleashCard);
        }
    }

    private static bool HasUnleashAvailable(Player owner)
    {
        return PileType.Hand.GetPile(owner).Cards.Any(IsUnleashCard)
               || PileType.Draw.GetPile(owner).Cards.Any(IsUnleashCard)
               || PileType.Discard.GetPile(owner).Cards.Any(IsUnleashCard);
    }

    private static bool IsUnleashCard(CardModel card)
    {
        var canonicalId = card.CanonicalInstance?.Id ?? card.Id;
        return canonicalId == ModelDb.Card<Unleash>().Id;
    }
}
