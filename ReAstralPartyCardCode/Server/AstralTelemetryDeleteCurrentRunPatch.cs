using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryDeleteCurrentRunPatch : IPatchMethod
{
    public static string PatchId => "astral_telemetry_delete_current_run_patch";
    public static bool IsCritical => false;
    public static string Description => "Discard Astral telemetry snapshot after deleting the saved current run";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(SaveManager), nameof(SaveManager.DeleteCurrentRun))];
    }

    public static void Postfix()
    {
        AstralTelemetry.DiscardPersistedRunStateIfNoActiveRun("delete_current_run");
    }
}
