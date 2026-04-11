using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NChooseARelicSelection), "_Ready")]
public static class ChooseRelicHeaderPatch
{
    [HarmonyPostfix]
    public static void Postfix(NChooseARelicSelection __instance)
    {
        var headerText = RelicSelectionHeaderContext.CurrentHeaderText;
        if (string.IsNullOrWhiteSpace(headerText))
            return;

        var banner = __instance.GetNodeOrNull<NCommonBanner>("Banner");
        banner?.label.SetTextAutoSize(headerText);
    }
}