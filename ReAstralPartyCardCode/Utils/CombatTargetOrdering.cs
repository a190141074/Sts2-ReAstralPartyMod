using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CombatTargetOrdering
{
    public static List<Creature> GetLivingOpponentsStable(Creature owner)
    {
        var combatState = owner.CombatState;
        if (combatState == null)
            return [];

        return combatState
            .GetOpponentsOf(owner)
            .Where(creature => creature.IsAlive)
            .OrderBy(creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(creature => creature.ModelId.ToString())
            .ThenBy(creature => creature.SlotName ?? string.Empty)
            .ToList();
    }
}
