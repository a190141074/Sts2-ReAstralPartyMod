using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryStartRunPatch : IPatchMethod
{
    public static string PatchId => "astral_telemetry_start_run_patch";
    public static bool IsCritical => false;
    public static string Description => "Reset Astral telemetry state after run start";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NGame), "StartRun", [typeof(RunState)])];
    }

    public static void Prefix(RunState runState)
    {
        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.StartRunBootstrap, "telemetry start run"))
            return;

        AstralTelemetry.BeginNewRun(runState);
    }
}
