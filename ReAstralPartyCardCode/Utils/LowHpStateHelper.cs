using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class LowHpStateHelper
{
    public static decimal GetCurrentHpRatio(Creature? creature)
    {
        if (creature == null || creature.MaxHp <= 0)
            return 0m;

        return (decimal)creature.CurrentHp / creature.MaxHp;
    }

    public static bool IsAtOrBelowHalfHp(Creature? creature)
    {
        return creature != null
               && creature.MaxHp > 0
               && GetCurrentHpRatio(creature) <= 0.5m;
    }

    public static bool IsBelowHalfHp(Creature? creature)
    {
        return creature != null
               && creature.MaxHp > 0
               && GetCurrentHpRatio(creature) < 0.5m;
    }

    public static bool IsBelowQuarterHp(Creature? creature)
    {
        return creature != null
               && creature.MaxHp > 0
               && GetCurrentHpRatio(creature) < 0.25m;
    }

    public static bool IsAboveHalfHp(Creature? creature)
    {
        return creature != null
               && creature.MaxHp > 0
               && GetCurrentHpRatio(creature) > 0.5m;
    }
}
