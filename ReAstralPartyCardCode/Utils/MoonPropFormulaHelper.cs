using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class MoonPropFormulaHelper
{
    public static decimal GetRepeatedMultiplier(decimal perStackMultiplier, int stacks)
    {
        var normalizedStacks = Math.Max(stacks, 0);
        var result = 1m;
        for (var i = 0; i < normalizedStacks; i++)
            result *= perStackMultiplier;

        return result;
    }

    public static decimal GetHalfDecayRatio(int stacks)
    {
        return GetRepeatedMultiplier(0.5m, Math.Max(stacks, 0));
    }

    public static decimal GetHalfDecayAccumulatedRatio(int stacks)
    {
        return 1m - GetHalfDecayRatio(Math.Max(stacks, 0));
    }

    public static decimal GetMoonPropStacks(MoonPropStackableRelicBase? relic)
    {
        return Math.Max(relic?.GetStacks() ?? 1, 1);
    }
}
