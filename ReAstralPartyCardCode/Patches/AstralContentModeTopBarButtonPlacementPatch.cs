using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static class AstralContentModeTopBarButtonPlacementPatch
{
    [HarmonyPostfix]
    public static void Postfix(NTopBar __instance, IRunState runState)
    {
        _ = __instance;
        _ = runState;
    }
}
