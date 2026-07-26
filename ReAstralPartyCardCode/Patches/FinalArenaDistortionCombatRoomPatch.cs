using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class FinalArenaDistortionCombatRoomPatch : IPatchMethod
{
    public static string PatchId => "final_arena_distortion_combat_room_patch";
    public static bool IsCritical => false;
    public static string Description =>
        "Install Final Arena's hidden combat modifier only when a combat room is about to create its combat state";

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(CombatRoom),
                ".ctor",
                [typeof(EncounterModel), typeof(IRunState)],
                HarmonyLib.MethodType.Constructor)
        ];
    }

    public static void Prefix(IRunState? runState)
    {
        if (runState is RunState concreteRunState)
            FinalArenaSeriesHelper.EnsureCombatModifierInstalled(concreteRunState);
    }
}
