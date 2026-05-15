using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.OnEnded), typeof(bool))]
public static class AstralTelemetryRunEndedPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunManager __instance, out RunState? __state)
    {
        __state = __instance.DebugOnlyGetState();
    }

    [HarmonyPostfix]
    public static void Postfix(RunState? __state, bool isVictory, SerializableRun __result)
    {
        AstralTelemetry.OnRunEnded(__state, __result, isVictory);
    }
}
