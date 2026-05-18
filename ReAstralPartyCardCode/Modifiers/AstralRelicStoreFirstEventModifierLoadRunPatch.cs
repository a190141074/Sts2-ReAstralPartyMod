using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;

[HarmonyPatch(typeof(NGame), "LoadRun", typeof(RunState), typeof(SerializableRoom))]
public static class AstralRelicStoreFirstEventModifierLoadRunPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterLoadRun(__result, runState);
    }

    private static async Task RunAfterLoadRun(Task originalTask, RunState runState)
    {
        await originalTask;
        AstralRelicStoreFirstEventModifierInstaller.EnsureInstalledForLoadedRun(runState);
    }
}
