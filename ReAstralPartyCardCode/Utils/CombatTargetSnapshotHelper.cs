using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class CombatTargetSnapshotHelper
{
    // Damage/death hooks can mutate CombatState creature collections mid-loop,
    // so callers that may kill/remove targets should iterate a stable snapshot first.
    public static IReadOnlyList<Creature> GetAliveOpponents(Creature ownerCreature)
    {
        return ownerCreature.CombatState?
            .GetOpponentsOf(ownerCreature)
            .Where(static creature => creature.IsAlive)
            .ToList() ?? [];
    }

    public static IReadOnlyList<Creature> GetAliveTeammates(Creature ownerCreature)
    {
        return ownerCreature.CombatState?
            .GetTeammatesOf(ownerCreature)
            .Where(static creature => creature.IsAlive)
            .ToList() ?? [];
    }

    public static IReadOnlyList<Creature> GetAliveNonAlliedCreatures(CombatState combatState, Creature ownerCreature)
    {
        return combatState.Creatures
            .Where(creature => creature.IsAlive && creature.Side != ownerCreature.Side)
            .ToList();
    }
}
