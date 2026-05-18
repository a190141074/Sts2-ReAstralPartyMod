using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

[HarmonyPatch(typeof(NGame), "StartRun")]
public static class AstralRelicStoreFirstEventModifierStartRunPatch
{
    [HarmonyPrefix]
    public static void Prefix(RunState runState)
    {
        AstralRelicStoreFirstEventModifierInstaller.EnsureInstalledForNewRun(runState);
    }
}
