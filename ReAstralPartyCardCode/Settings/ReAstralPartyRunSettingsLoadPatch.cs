using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

public sealed class ReAstralPartyRunSettingsLoadPatch : IPatchMethod
{
    public static string PatchId => "re_astral_party_run_settings_load_patch";
    public static bool IsCritical => false;
    public static string Description => "Ensure run-scoped Astral Party settings are initialized after loading a run";

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(NGame), "LoadRun", [typeof(RunState), typeof(SerializableRoom)])];
    }

    public static void Postfix(RunState runState, ref Task __result)
    {
        __result = RunAfterLoadRun(__result, runState);
    }

    private static async Task RunAfterLoadRun(Task originalTask, RunState runState)
    {
        await originalTask;
        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.MapOrRoom, "run settings load sync"))
            return;

        DevConsoleCommandNotificationSync.Register();
        AstralDlcOverrideSync.Register();
        FinalArenaDistortionComponentSync.Register();
        await ReAstralPartyRunSettingsSync.EnsureSyncedAsync(runState);

        var netRole = LobbyGameplayNetRoleHelper.GetCurrentRole();
        if (netRole == LobbyGameplayNetRole.Client)
            await FinalArenaSeriesHelper.EnsureInitializedAndAppliedAsync(runState, false, false);
        else
            await FinalArenaSeriesHelper.EnsureInitializedAndAppliedAsync(runState, true, true);
    }
}
