using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.Abandon))]
public static class AstralTelemetryAbandonPatch
{
    [HarmonyPrefix]
    public static void Prefix()
    {
        AstralTelemetry.DiscardPersistedRunState("abandon_run");
    }
}
