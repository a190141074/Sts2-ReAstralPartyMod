using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NGame), "StartRun")]
public static class AstralTelemetryStartRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunState runState)
    {
        AstralTelemetry.ResetRunState(runState);
    }
}
