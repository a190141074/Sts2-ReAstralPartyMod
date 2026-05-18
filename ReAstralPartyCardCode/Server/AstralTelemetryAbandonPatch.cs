using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryAbandonPatch : IPatchMethod
{
    public static string PatchId => "astral_telemetry_abandon_patch";
    public static bool IsCritical => false;
    public static string Description => "Discard Astral telemetry snapshot when the active run is abandoned";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RunManager), nameof(RunManager.Abandon))];
    }

    public static void Prefix()
    {
        AstralTelemetry.DiscardPersistedRunState("abandon_run");
    }
}
