using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

[HarmonyPatch(typeof(NGame), "LoadRun", typeof(RunState), typeof(SerializableRoom))]
public static class ReAstralPartyRunSettingsLoadPatch
{
    [HarmonyPostfix]
    public static void Postfix(RunState runState)
    {
        _ = ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);
    }
}
