using AstralPartyMod.AstralPartyCardCode.cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.Title), MethodType.Getter)]
//名刀动态标题补丁
public static class SkillFamousBladeTitlePatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (__instance is not SkillFamousBlade famousBlade) return;

        var language = LocManager.Instance?.Language ?? "eng";
        __result = famousBlade.GetDisplayTitle(language);
    }
}