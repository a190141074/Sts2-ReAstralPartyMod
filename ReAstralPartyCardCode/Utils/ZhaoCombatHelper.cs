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

        var attackCard = FindRandomAttackCard(owner);
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

    private static CardModel? FindRandomAttackCard(Player owner)
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
            return drawPileAttacks[owner.RunState.Rng.CombatTargets.NextInt(drawPileAttacks.Count)];

        var discardPileAttacks = PileType.Discard
            .GetPile(owner)
            .Cards
            .Where(card => card.Type == CardType.Attack)
            .ToList();
        if (discardPileAttacks.Count > 0)
            return discardPileAttacks[owner.RunState.Rng.CombatTargets.NextInt(discardPileAttacks.Count)];

        return null;
    }
}
