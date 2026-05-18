using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryRunEndedPatch : IPatchMethod
{
    public static string PatchId => "astral_telemetry_run_ended_patch";
    public static bool IsCritical => false;
    public static string Description => "Submit Astral telemetry when a run ends";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), nameof(RunManager.OnEnded), [typeof(bool)])];
    }

    public static void Prefix(RunManager __instance, out RunState? __state)
    {
        __state = __instance.DebugOnlyGetState();
    }

    public static void Postfix(RunState? __state, bool isVictory, SerializableRun __result)
    {
        AstralTelemetry.OnRunEnded(__state, __result, isVictory);
    }
}
