using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class JunkBotQuestIconMarkedVisibilityPatch
{
    public static void Postfix(NNormalMapPoint __instance)
    {
        JunkBotQuestIconHelper.TryApplyMapQuestIcon(__instance, false);
    }
}
