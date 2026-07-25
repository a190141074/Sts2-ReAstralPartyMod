using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

internal static class FinalArenaPersonaCooldownAdjustmentHelper
{
    private const int DeityLinBaseMaxCounter = 4;

    public static void AdvanceCooldownRelicsFromDistortion(Player owner, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var relic in owner.Relics)
            switch (relic)
            {
                case PersonBionicJasmine bionicJasmine:
                    bionicJasmine.AddSteps(amount * 3);
                    break;
                case PersonDeityLin deityLin:
                    AdvanceDeityLinCooldownFromDistortion(deityLin, amount);
                    break;
                case CooldownPersonRelicBase cooldownRelic:
                    cooldownRelic.AdvanceCooldownProgressFromExternalEffect(amount);
                    break;
            }
    }

    public static int ApplyCooldownMaxCounterAdjustment(Player? owner, PersonRelicBase? sourceRelic, int adjustedCounter)
    {
        if (owner == null ||
            sourceRelic == null ||
            !FinalArenaSeriesHelper.HasComponent(owner.RunState, FinalArenaDistortionComponent.Overload))
            return adjustedCounter;

        return Math.Max(1, adjustedCounter - 1);
    }

    public static int ApplyBionicJasmineStepThresholdAdjustment(Player? owner, int adjustedThreshold)
    {
        if (owner == null ||
            !FinalArenaSeriesHelper.HasComponent(owner.RunState, FinalArenaDistortionComponent.Overload))
            return adjustedThreshold;

        return ApplyExtraBatteryStyleThresholdReduction(adjustedThreshold);
    }

    private static void AdvanceDeityLinCooldownFromDistortion(PersonDeityLin deityLin, int amount)
    {
        if (amount <= 0 ||
            deityLin.Owner == null ||
            deityLin.AstralParty_PersonDeityLinPendingCombatStartCard)
            return;

        var maxCounter = GetAdjustedDeityLinMaxCounter(deityLin.Owner);
        var cappedProgress = Math.Max(1, maxCounter - 1);
        if (deityLin.AstralParty_PersonDeityLinCounter >= cappedProgress)
            return;

        deityLin.AstralParty_PersonDeityLinCounter = Math.Min(
            deityLin.AstralParty_PersonDeityLinCounter + amount,
            cappedProgress);
    }

    private static int GetAdjustedDeityLinMaxCounter(Player owner)
    {
        var adjustedCounter = owner.GetRelic<TokenGoldExtraBattery>() != null
            ? DeityLinBaseMaxCounter - 1
            : DeityLinBaseMaxCounter;

        if (FinalArenaSeriesHelper.HasComponent(owner.RunState, FinalArenaDistortionComponent.Overload))
            adjustedCounter--;

        return Math.Max(1, adjustedCounter);
    }

    private static int ApplyExtraBatteryStyleThresholdReduction(int threshold)
    {
        return Math.Max(1, (int)Math.Ceiling(threshold * 0.75m));
    }
}
