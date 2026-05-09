using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NNormalMapPoint), "_Ready")]
public static class JunkBotQuestIconMapPatch
{
    [HarmonyPostfix]
    public static void Postfix(NNormalMapPoint __instance)
    {
        if (!JunkBotQuestIconHelper.TryApplyMapQuestIcon(__instance))
            return;

        MainFile.Logger.Info($"Junk Bot map quest icon replaced | path={JunkBotQuestIconHelper.IconPath}");
    }
}
