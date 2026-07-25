using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class ExtraBatteryRelicHelper
{
    public static int GetAdjustedCooldownMaxCounter(Player? owner, int baseMaxCounter, PersonRelicBase? sourceRelic = null)
    {
        var adjustedCounter = baseMaxCounter;

        if (owner?.GetRelic<Relics.TokenGoldExtraBattery>() != null)
            adjustedCounter--;

        if (sourceRelic is VariantPersonSara && owner?.GetRelic<JewelryNightSkin>() != null)
            adjustedCounter -= 2;

        if (sourceRelic is VariantPersonSinkou && owner?.GetRelic<JewelryEchoOfDivineLight>() != null)
            adjustedCounter -= SinkouSetHelper.GetCurrentAct(owner) >= 2 ? 2 : 1;

        return FinalArenaPersonaCooldownAdjustmentHelper.ApplyCooldownMaxCounterAdjustment(
            owner,
            sourceRelic,
            Math.Max(1, adjustedCounter));
    }

    public static int GetAdjustedBionicJasmineStepThreshold(Player? owner, int baseThreshold)
    {
        var adjustedThreshold = owner?.GetRelic<Relics.TokenGoldExtraBattery>() == null
            ? baseThreshold
            : Math.Max(1, (int)Math.Ceiling(baseThreshold * 0.75m));

        return FinalArenaPersonaCooldownAdjustmentHelper.ApplyBionicJasmineStepThresholdAdjustment(
            owner,
            adjustedThreshold);
    }
}
