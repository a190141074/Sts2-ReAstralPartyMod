using System.Linq;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class WrongTargetPower : BaseAbilityRetaliationPowerBase
{
    protected override Creature? ResolveRetaliationTarget(Creature source)
    {
        var combatState = Owner?.CombatState;
        if (Owner == null || combatState == null)
            return source;

        var candidates = CombatTargetOrdering.GetLivingOpponentsStable(Owner)
            .Where(creature => creature != source)
            .ToList();
        if (candidates.Count == 0)
            return source;

        var rng = Owner.Player?.RunState?.Rng?.CombatTargets;
        if (rng == null)
            return candidates[0];

        return candidates[rng.NextInt(candidates.Count)];
    }
}
