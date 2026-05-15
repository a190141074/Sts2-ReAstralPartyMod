using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DeleteCurrentRun))]
public static class AstralTelemetryDeleteCurrentRunPatch
{
    [HarmonyPostfix]
    public static void Postfix()
    {
        AstralTelemetry.DiscardPersistedRunStateIfNoActiveRun("delete_current_run");
    }
}
