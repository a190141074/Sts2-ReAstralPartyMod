using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

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
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (drawPileAttacks.Count > 0)
            return DeterministicMultiplayerChoiceHelper.PickDeterministically(
                drawPileAttacks,
                GetStableAttackCardKey,
                MainFile.ModId,
                "zhao_chase",
                source.Id.Entry,
                owner.RunState.Rng.StringSeed,
                owner.NetId,
                combatState.RoundNumber,
                bonusDamage,
                PileType.Draw);

        var discardPileAttacks = PileType.Discard
            .GetPile(owner)
            .Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (discardPileAttacks.Count > 0)
            return DeterministicMultiplayerChoiceHelper.PickDeterministically(
                discardPileAttacks,
                GetStableAttackCardKey,
                MainFile.ModId,
                "zhao_chase",
                source.Id.Entry,
                owner.RunState.Rng.StringSeed,
                owner.NetId,
                combatState.RoundNumber,
                bonusDamage,
                PileType.Discard);

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
        {
            if (ReferenceEquals(cards[i], target))
                return i;
        }

        return -1;
    }
}
