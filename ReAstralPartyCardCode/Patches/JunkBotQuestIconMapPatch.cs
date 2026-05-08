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
        if (!JunkBotQuestIconHelper.HasJunkBotQuest(__instance.Point))
            return;

        var questIcon = __instance.GetNodeOrNull<TextureRect>("%QuestIcon");
        var texture = JunkBotQuestIconHelper.LoadTexture();
        if (questIcon == null || texture == null)
        {
            MainFile.Logger.Warn(
                $"Junk Bot map quest icon patch skipped | questIconFound={questIcon != null} | textureLoaded={texture != null} | path={JunkBotQuestIconHelper.IconPath}");
            return;
        }

        questIcon.Texture = texture;
        MainFile.Logger.Info(
            $"Junk Bot map quest icon replaced | path={JunkBotQuestIconHelper.IconPath}");
    }
}
