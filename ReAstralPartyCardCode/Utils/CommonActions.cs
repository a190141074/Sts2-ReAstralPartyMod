using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CommonActions
{
    public static AttackCommand CardAttack(CardModel card, CardPlay cardPlay, int hitCount = 1)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(card.Owner?.Creature);

        return new AttackCommand(card.DynamicVars.Damage.BaseValue)
            .FromCard(card)
            .Targeting(cardPlay.Target)
            .WithHitCount(hitCount);
    }

    public static async Task AttackAllEnemies(PlayerChoiceContext choiceContext, CardModel card, int hitCount = 1)
    {
        if (card.Owner?.Creature?.CombatState == null)
            return;

        var enemies = card.Owner.Creature.CombatState
            .GetOpponentsOf(card.Owner.Creature)
            .Where(static creature => creature.IsAlive)
            .ToList();
        foreach (var enemy in enemies)
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                card.DynamicVars.Damage.BaseValue,
                ValueProp.Move,
                card.Owner.Creature,
                card);
    }
}
