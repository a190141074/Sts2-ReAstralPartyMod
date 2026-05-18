using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryLoadRunPatch : IPatchMethod
{
    public static string PatchId => "astral_telemetry_load_run_patch";
    public static bool IsCritical => false;
    public static string Description => "Restore Astral telemetry state after loading a run";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NGame), "LoadRun", [typeof(RunState), typeof(SerializableRoom)])];
    }

    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterLoadRun(__result, runState);
    }

    private static async Task RunAfterLoadRun(Task originalTask, RunState runState)
    {
        await originalTask;
        AstralTelemetry.RestoreLoadedRun(runState);
    }
}
