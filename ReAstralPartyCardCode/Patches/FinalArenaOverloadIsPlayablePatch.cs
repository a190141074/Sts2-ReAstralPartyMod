using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class FinalArenaOverloadIsPlayablePatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result)
            return;

        if (FinalArenaDistortionComponentModifier.ShouldBlockOverloadManualPlay(__instance))
            __result = false;
    }
}
