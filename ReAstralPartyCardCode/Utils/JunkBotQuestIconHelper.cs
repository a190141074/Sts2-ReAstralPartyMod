using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.RunHistoryScreen;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using HarmonyLib;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class JunkBotQuestIconHelper
{
    private const string QuestIconPath = "res://ReAstralPartyMod/images/quest/quest_z3000.png";

    private static readonly AccessTools.FieldRef<NMapPointHistoryEntry, MapPointHistoryEntry> EntryField =
        AccessTools.FieldRefAccess<NMapPointHistoryEntry, MapPointHistoryEntry>("_entry");

    private static readonly AccessTools.FieldRef<NMapPointHistoryEntry, RunHistoryPlayer?> PlayerField =
        AccessTools.FieldRefAccess<NMapPointHistoryEntry, RunHistoryPlayer?>("_player");

    private static bool _loadLogged;

    public static string IconPath => QuestIconPath;

    public static Texture2D? LoadTexture()
    {
        var texture = PreloadManager.Cache.GetCompressedTexture2D(QuestIconPath);
        if (!_loadLogged)
        {
            _loadLogged = true;
            MainFile.Logger.Info(
                $"Junk Bot quest icon load | path={QuestIconPath} | success={texture != null}");
        }

        return texture;
    }

    public static bool HasJunkBotQuest(MapPoint? point)
    {
        return point?.Quests.Any(IsJunkBotQuestModel) == true;
    }

    public static bool TryApplyMapQuestIcon(NNormalMapPoint mapPointNode, bool logSkipped = true)
    {
        if (!HasJunkBotQuest(mapPointNode.Point))
            return false;

        var questIcon = mapPointNode.GetNodeOrNull<TextureRect>("%QuestIcon");
        var texture = LoadTexture();
        if (questIcon == null || texture == null)
        {
            if (logSkipped)
                MainFile.Logger.Warn(
                    $"Junk Bot map quest icon patch skipped | questIconFound={questIcon != null} | textureLoaded={texture != null} | path={IconPath}");

            return false;
        }

        questIcon.Texture = texture;
        return true;
    }

    public static bool HasJunkBotCompletedQuest(NMapPointHistoryEntry entry)
    {
        var player = PlayerField(entry);
        var historyEntry = EntryField(entry);
        if (player == null || historyEntry == null)
            return false;

        return historyEntry
            .GetEntry(player.Id)
            .CompletedQuests
            .Any(id => id == ModelDb.Relic<PersonJunkBot>().Id);
    }

    private static bool IsJunkBotQuestModel(AbstractModel model)
    {
        return model is PersonJunkBot || model.Id == ModelDb.Relic<PersonJunkBot>().Id;
    }
}
