using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CardModelIsValidTargetMixedSinglePatch : IPatchMethod
{
    public static string PatchId => "card_mixed_single_is_valid_target";
    public static string Description => "Allow SkillFortuneMischance to validate both player and enemy targets";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CardModel), nameof(CardModel.IsValidTarget), [typeof(Creature)])];
    }

    public static bool Prefix(CardModel __instance, Creature? target, ref bool __result)
    {
        if (!MixedSingleTargetingRuntime.IsMixedSingleTargetCard(__instance))
            return true;

        if (target == null)
        {
            __result = false;
            return false;
        }

        __result = MixedSingleTargetingRuntime.IsValidTarget(target);
        return false;
    }
}
