using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(RelicGrabBag), nameof(RelicGrabBag.Populate),
    [typeof(Player), typeof(MegaCrit.Sts2.Core.Random.Rng)])]
internal static class RelicGrabBagPopulateSeriesFilterPatch
{
    [HarmonyPostfix]
    public static void Postfix(RelicGrabBag __instance, Player player)
    {
        if (player?.RunState == null)
            return;

        RemoveLockedSeriesRelics(__instance, player.RunState);
        CursedScrollGrabBagHelper.Normalize(__instance, player);
        AstralNeowDiagnosticHelper.ReportGrabBagRaritySnapshot(__instance, player);
    }

    private static void RemoveLockedSeriesRelics(RelicGrabBag grabBag, IRunState runState)
    {
        foreach (var relic in TokenRelicRegistry.GetCanonicalTokenRelics())
        {
            if (!TokenRelicRegistry.IsSeriesTokenRelic(relic))
                continue;
            if (TokenRelicRegistry.IsRelicAvailableForRun(runState, relic))
                continue;

            grabBag.Remove(relic);
        }

        foreach (var relic in BannedRelicRegistry.GetCanonicalBannableRelics())
        {
            if (AstralRelicAvailabilityHelper.IsRelicEnabledForRun(runState, relic))
                continue;

            grabBag.Remove(relic);
        }
    }
}
