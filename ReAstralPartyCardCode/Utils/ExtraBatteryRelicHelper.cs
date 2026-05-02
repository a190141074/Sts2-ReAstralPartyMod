using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ExtraBatteryRelicHelper
{
    public static int GetAdjustedCooldownMaxCounter(Player? owner, int baseMaxCounter)
    {
        if (owner?.GetRelic<Relics.TokenGoldExtraBattery>() == null)
            return baseMaxCounter;

        return Math.Max(1, baseMaxCounter - 1);
    }

    public static int GetAdjustedBionicJasmineStepThreshold(Player? owner, int baseThreshold)
    {
        if (owner?.GetRelic<Relics.TokenGoldExtraBattery>() == null)
            return baseThreshold;

        return Math.Max(1, (int)Math.Ceiling(baseThreshold * 0.75m));
    }
}