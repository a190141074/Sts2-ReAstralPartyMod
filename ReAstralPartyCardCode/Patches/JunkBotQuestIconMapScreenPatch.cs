using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public static class JunkBotQuestIconMapScreenPatch
{
    public static void Postfix(NMapScreen __instance)
    {
        try
        {
            var appliedCount = JunkBotQuestIconHelper.ReapplyMarkedQuestsAndIcons(__instance);

            if (appliedCount > 0)
                MainFile.Logger.Info(
                    $"Junk Bot map screen refresh reapplied quest icons | count={appliedCount} | path={JunkBotQuestIconHelper.IconPath}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Junk Bot map screen refresh skipped after exception: {ex}");
        }
    }
}
