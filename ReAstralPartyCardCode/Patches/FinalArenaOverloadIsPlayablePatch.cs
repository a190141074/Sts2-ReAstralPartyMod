using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class FinalArenaOverloadIsPlayablePatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (VariantPersonNinja.IsVariantNinjaSkill(__instance))
        {
            if (__instance.Owner?.GetRelic<VariantPersonNinja>() is { } ninja
                && ninja.ShouldBlockSkillPlay(__instance))
                __result = false;

            return;
        }

        if (!__result)
            return;

        if (FinalArenaDistortionComponentModifier.ShouldBlockOverloadManualPlay(__instance))
            __result = false;
    }
}
