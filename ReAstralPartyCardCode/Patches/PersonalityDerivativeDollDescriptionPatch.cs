using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class PersonalityDerivativeDollDescriptionPatch
{
    public static void Postfix(RelicModel __instance, ref LocString __result)
    {
        if (__instance is PersonalityDerivativeDollMaking doll)
            __result = new LocString("relics", doll.IsComplete
                ? "RE_ASTRAL_PARTY_MOD_RELIC_PERSONALITY_DERIVATIVE_DOLL_COMPLETE.description"
                : "RE_ASTRAL_PARTY_MOD_RELIC_PERSONALITY_DERIVATIVE_DOLL_MAKING.description");
    }
}
