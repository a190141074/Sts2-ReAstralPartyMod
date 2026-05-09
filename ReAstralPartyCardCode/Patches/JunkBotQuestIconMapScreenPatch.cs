using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class JunkBotQuestIconMapScreenPatch
{
    public static void Postfix(NMapScreen __instance)
    {
        var appliedCount = 0;
        foreach (var child in __instance.FindChildren("*", nameof(NNormalMapPoint), true, false))
        {
            if (child is not NNormalMapPoint mapPointNode)
                continue;

            if (JunkBotQuestIconHelper.TryApplyMapQuestIcon(mapPointNode, logSkipped: false))
                appliedCount++;
        }

        if (appliedCount > 0)
        {
            MainFile.Logger.Info(
                $"Junk Bot map screen refresh reapplied quest icons | count={appliedCount} | path={JunkBotQuestIconHelper.IconPath}");
        }
    }
}
