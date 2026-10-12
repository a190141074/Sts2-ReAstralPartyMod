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

    public static List<Creature> GetNearestLivingSameSideStable(Creature anchor, int maxCount)
    {
        if (anchor.CombatState == null || maxCount <= 0)
            return [];

        var sameSide = anchor.CombatState.GetCreaturesOnSide(anchor.Side).ToList();
        var anchorIndex = sameSide.IndexOf(anchor);
        if (anchorIndex < 0)
            return [];

        return sameSide
            .Select((creature, index) => new { Creature = creature, Distance = Math.Abs(index - anchorIndex) })
            .Where(entry => entry.Creature != anchor && entry.Creature.IsAlive)
            .OrderBy(entry => entry.Distance)
            .ThenBy(entry => entry.Creature.CombatId ?? uint.MaxValue)
            .ThenBy(entry => entry.Creature.ModelId.ToString())
            .ThenBy(entry => entry.Creature.SlotName ?? string.Empty)
            .Take(maxCount)
            .Select(entry => entry.Creature)
            .ToList();
    }
}
