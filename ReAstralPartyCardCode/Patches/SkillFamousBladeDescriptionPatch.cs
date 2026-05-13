using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Description), MethodType.Getter)]
// Famous Blade changes description based on the current Sword Intent tier.
public static class SkillFamousBladeDescriptionPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (__instance is not SkillFamousBlade famousBlade)
            return;

        var key = famousBlade.GetDisplayDescriptionKey();
        if (string.IsNullOrWhiteSpace(key))
            return;

        __result = new LocString("cards", key);
    }
}
