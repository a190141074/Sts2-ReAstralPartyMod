using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class JunkBotQuestIconTravelabilityPatch
{
    public static void Postfix(NMapScreen __instance)
    {
        var appliedCount = JunkBotQuestIconHelper.ReapplyMarkedQuestsAndIcons(__instance);
        if (appliedCount <= 0)
            return;

        JunkBotQuestIconHelper.RefreshMapPointVisuals(__instance);
    }
}
