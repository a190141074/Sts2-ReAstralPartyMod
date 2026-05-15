using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NGame), "LoadRun", typeof(RunState), typeof(SerializableRoom))]
public static class AstralTelemetryLoadRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunState runState)
    {
        AstralTelemetry.RestoreLoadedRun(runState);
    }
}
