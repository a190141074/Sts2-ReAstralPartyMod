using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public sealed class ReAstralPartyRunSettingsStartPatch : IPatchMethod
{
    public static string PatchId => "re_astral_party_run_settings_start_patch";
    public static bool IsCritical => false;

    public static string Description =>
        "Initialize Final Arena distortion components for fresh runs before startup UI reads them";

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NGame), "StartRun", [typeof(RunState)])];
    }

    public static void Postfix(RunState runState, ref Task __result)
    {
        FinalArenaSeriesHelper.EnsureInitializedForUi(runState);
        __result = RunAfterStartRun(__result, runState);
    }

    private static async Task RunAfterStartRun(Task originalTask, RunState runState)
    {
        await originalTask;

        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.StartRunBootstrap, "final arena distortion bootstrap"))
            return;

        AstralDlcOverrideSync.Register();
        FinalArenaDistortionComponentSync.Register();
        await ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);

        var netRole = LobbyGameplayNetRoleHelper.GetCurrentRole();
        await FinalArenaSeriesHelper.EnsureInitializedAndAppliedAsync(
            runState,
            netRole != LobbyGameplayNetRole.Client,
            true);
    }
}
