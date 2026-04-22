using AstralPartyMod.AstralPartyCardCode.Keywords;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel))]
public static class CooldownKeywordPatch
{
    [HarmonyPatch(nameof(CardModel.ExhaustOnNextPlay), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ExhaustOnNextPlayPostfix(CardModel __instance, ref bool __result)
    {
        if (__result)
            return;

        if (__instance.Keywords.Contains(AstralKeywords.AstralCooldown))
            __result = true;
    }

    [HarmonyPatch(nameof(CardModel.ShouldRetainThisTurn), MethodType.Getter)]
    [HarmonyPostfix]
    public static void ShouldRetainThisTurnPostfix(CardModel __instance, ref bool __result)
    {
        if (__result)
            return;

        if (__instance.Keywords.Contains(AstralKeywords.AstralCooldown))
            __result = true;
    }
}
