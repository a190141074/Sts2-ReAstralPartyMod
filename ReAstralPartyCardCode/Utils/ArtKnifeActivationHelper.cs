using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class ArtKnifeActivationHelper
{
    private const decimal LingYulinActivationThreshold = 0.8m;

    public static bool IsActivationSatisfied(Creature? creature)
    {
        if (creature == null || creature.MaxHp <= 0m)
            return false;

        return HasLingYulinThreshold(creature)
            ? creature.CurrentHp > creature.MaxHp * LingYulinActivationThreshold
            : creature.CurrentHp >= creature.MaxHp;
    }

    public static bool HasLingYulinThreshold(Creature? creature)
    {
        return creature?.Player?.GetRelic<VariantPersonLingYulin>() != null;
    }
}
