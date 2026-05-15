using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
// Famous Blade changes title based on the current Sword Intent tier.
public static class SkillFamousBladeTitlePatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not SkillFamousBlade famousBlade)
            return;

        var language = LocManager.Instance?.Language ?? "eng";
        __result = famousBlade.GetDisplayTitle(language);
    }
}
