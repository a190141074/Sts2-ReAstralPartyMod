using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NMapPointHistoryEntry), nameof(NMapPointHistoryEntry.SetPlayer))]
public static class JunkBotQuestIconHistoryPatch
{
    [HarmonyPostfix]
    public static void Postfix(NMapPointHistoryEntry __instance)
    {
        if (!JunkBotQuestIconHelper.HasJunkBotCompletedQuest(__instance))
            return;

        var questIcon = __instance.GetNodeOrNull<TextureRect>("%QuestIcon");
        var texture = JunkBotQuestIconHelper.LoadTexture();
        if (questIcon == null || texture == null)
        {
            MainFile.Logger.Warn(
                $"Junk Bot history quest icon patch skipped | questIconFound={questIcon != null} | textureLoaded={texture != null} | path={JunkBotQuestIconHelper.IconPath}");
            return;
        }

        questIcon.Texture = texture;
        MainFile.Logger.Info(
            $"Junk Bot history quest icon replaced | path={JunkBotQuestIconHelper.IconPath}");
    }
}
