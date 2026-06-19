using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class EventCombatTargetHelper
{
    public static IReadOnlyList<Player> GetAlivePlayers(ICombatState combatState)
    {
        return combatState.Players
            .Where(static player => player.Creature != null && player.Creature.IsAlive)
            .ToList();
    }

    public static IReadOnlyList<Creature> GetAlivePlayerCreatures(ICombatState combatState)
    {
        return GetAlivePlayers(combatState)
            .Select(static player => player.Creature!)
            .ToList();
    }

    public static IReadOnlyList<Creature> GetAliveCreaturesExcludingPlayerSummons(ICombatState combatState)
    {
        return combatState.Creatures
            .Where(static creature => creature.IsAlive && creature.PetOwner == null)
            .ToList();
    }

    public static IReadOnlyList<Creature> GetAliveNonSummonEnemies(ICombatState combatState, Creature ownerCreature)
    {
        return combatState.Creatures
            .Where(creature => creature.IsAlive
                               && creature.PetOwner == null
                               && creature.Side != ownerCreature.Side)
            .ToList();
    }
}
