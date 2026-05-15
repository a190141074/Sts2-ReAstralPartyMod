using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Lobby;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.TreasureRoomRelic;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class ExpandedMultiplayerCompatibilityPatch
{
    public const int ExpandedPlayerSlotBitCount = 4;
    public const int ExpandedLobbyPlayerListBitCount = 4;

    public static IEnumerable<CodeInstruction> ReplaceIntConstant(
        IEnumerable<CodeInstruction> instructions,
        int from,
        int to)
    {
        foreach (var instruction in instructions)
            if (TryGetLdcI4Value(instruction, out var value) && value == from)
                yield return new CodeInstruction(OpCodes.Ldc_I4, to);
            else
                yield return instruction;
    }

    private static bool TryGetLdcI4Value(CodeInstruction instruction, out int value)
    {
        if (instruction.opcode == OpCodes.Ldc_I4_M1)
        {
            value = -1;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_0)
        {
            value = 0;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_1)
        {
            value = 1;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_2)
        {
            value = 2;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_3)
        {
            value = 3;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_4)
        {
            value = 4;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_5)
        {
            value = 5;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_6)
        {
            value = 6;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_7)
        {
            value = 7;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_8)
        {
            value = 8;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte shortValue)
        {
            value = shortValue;
            return true;
        }

        if (instruction.opcode == OpCodes.Ldc_I4 && instruction.operand is int intValue)
        {
            value = intValue;
            return true;
        }

        value = 0;
        return false;
    }
}

[HarmonyPatch(typeof(LobbyPlayer), nameof(LobbyPlayer.Serialize))]
internal static class LobbyPlayerSerializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            2,
            ExpandedMultiplayerCompatibilityPatch.ExpandedPlayerSlotBitCount);
    }
}

[HarmonyPatch(typeof(LobbyPlayer), nameof(LobbyPlayer.Deserialize))]
internal static class LobbyPlayerDeserializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            2,
            ExpandedMultiplayerCompatibilityPatch.ExpandedPlayerSlotBitCount);
    }
}

[HarmonyPatch(typeof(ClientLobbyJoinResponseMessage), nameof(ClientLobbyJoinResponseMessage.Serialize))]
internal static class ClientLobbyJoinResponseSerializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            3,
            ExpandedMultiplayerCompatibilityPatch.ExpandedLobbyPlayerListBitCount);
    }
}

[HarmonyPatch(typeof(ClientLobbyJoinResponseMessage), nameof(ClientLobbyJoinResponseMessage.Deserialize))]
internal static class ClientLobbyJoinResponseDeserializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            3,
            ExpandedMultiplayerCompatibilityPatch.ExpandedLobbyPlayerListBitCount);
    }
}

[HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Serialize))]
internal static class LobbyBeginRunSerializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            3,
            ExpandedMultiplayerCompatibilityPatch.ExpandedLobbyPlayerListBitCount);
    }
}

[HarmonyPatch(typeof(LobbyBeginRunMessage), nameof(LobbyBeginRunMessage.Deserialize))]
internal static class LobbyBeginRunDeserializeExpandedBitWidthPatch
{
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        return ExpandedMultiplayerCompatibilityPatch.ReplaceIntConstant(
            instructions,
            3,
            ExpandedMultiplayerCompatibilityPatch.ExpandedLobbyPlayerListBitCount);
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
internal static class NRestSiteRoomExpandedPlayerPatch
{
    private static readonly MethodInfo? CharacterContainerGetter =
        AccessTools.PropertyGetter(typeof(List<Control>), "Item");

    private static readonly MethodInfo? SafeContainerGetter =
        AccessTools.Method(typeof(NRestSiteRoomExpandedPlayerPatch), nameof(GetOrCreateCharacterContainer));

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
            if (CharacterContainerGetter != null
                && SafeContainerGetter != null
                && instruction.Calls(CharacterContainerGetter))
                yield return new CodeInstruction(OpCodes.Call, SafeContainerGetter);
            else
                yield return instruction;
    }

    public static Control GetOrCreateCharacterContainer(List<Control> containers, int index)
    {
        if (containers.Count == 0)
            throw new InvalidOperationException("No rest site character containers are available.");

        while (containers.Count <= index)
            containers.Add(CreateCharacterContainer(containers));

        ApplyCharacterContainerVisibility(containers);
        return containers[index];
    }

    private static Control CreateCharacterContainer(List<Control> containers)
    {
        var template = containers[^1];
        var parent = template.GetParent<Control>()
                     ?? throw new InvalidOperationException("Rest site character container parent is missing.");
        var clone = template.Duplicate(15) as Control ?? new Control();

        clone.Name = $"Character_{containers.Count + 1}";
        clone.Position = template.Position;
        clone.Scale = template.Scale;
        clone.Visible = true;

        parent.AddChildSafely(clone);
        return clone;
    }

    private static void ApplyCharacterContainerVisibility(List<Control> containers)
    {
        for (var i = 0; i < containers.Count; i++)
            containers[i].Visible = i < 4;
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "OnPlayerChangedHoveredRestSiteOption")]
internal static class NRestSiteRoomHoverExpandedPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRestSiteRoom __instance, ulong playerId)
    {
        var character = __instance.Characters.FirstOrDefault(candidate => candidate.Player.NetId == playerId);
        if (character == null)
            return false;

        var hoveredOptionIndex = RunManager.Instance.RestSiteSynchronizer.GetHoveredOptionIndex(playerId);
        RestSiteOption? option = null;
        if (hoveredOptionIndex.HasValue)
        {
            var options = RunManager.Instance.RestSiteSynchronizer.GetOptionsForPlayer(playerId);
            if (hoveredOptionIndex.Value >= 0 && hoveredOptionIndex.Value < options.Count)
                option = options[hoveredOptionIndex.Value];
        }

        character.ShowHoveredRestSiteOption(option);
        return false;
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "OnBeforePlayerSelectedRestSiteOption")]
internal static class NRestSiteRoomBeforeSelectExpandedPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRestSiteRoom __instance, RestSiteOption option, ulong playerId)
    {
        var character = __instance.Characters.FirstOrDefault(candidate => candidate.Player.NetId == playerId);
        character?.SetSelectingRestSiteOption(option);
        return false;
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), "OnAfterPlayerSelectedRestSiteOption")]
internal static class NRestSiteRoomAfterSelectExpandedPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NRestSiteRoom __instance, RestSiteOption option, bool success, ulong playerId)
    {
        var character = __instance.Characters.FirstOrDefault(candidate => candidate.Player.NetId == playerId);
        if (character == null)
            return false;

        character.SetSelectingRestSiteOption(null);
        if (success)
        {
            character.ShowSelectedRestSiteOption(option);
            if (!LocalContext.IsMe(character.Player))
                TaskHelper.RunSafely(option.DoRemotePostSelectVfx());
        }

        return false;
    }
}

[HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.InitializeRelics))]
internal static class NTreasureRoomRelicCollectionExpandedPlayerPatch
{
    [HarmonyPrefix]
    public static void Prefix(NTreasureRoomRelicCollection __instance)
    {
        Traverse.Create(__instance)
            .Field<List<NTreasureRoomRelicHolder>>("_holdersInUse")
            .Value?
            .Clear();

        var holders = Traverse.Create(__instance)
            .Field<List<NTreasureRoomRelicHolder>>("_multiplayerHolders")
            .Value;
        var currentRelics = RunManager.Instance.TreasureRoomRelicSynchronizer.CurrentRelics;

        if (holders == null
            || holders.Count == 0
            || currentRelics == null
            || currentRelics.Count <= holders.Count)
            return;

        var template = holders[^1];
        var parent = template.GetParent();
        if (parent == null)
            return;

        for (var index = holders.Count; index < currentRelics.Count; index++)
        {
            if (template.Duplicate(15) is not NTreasureRoomRelicHolder clone)
                continue;

            clone.Name = $"AutoHolder_{index + 1}";
            clone.Visible = false;
            parent.AddChild(clone);
            holders.Add(clone);
        }
    }

    [HarmonyPostfix]
    public static void Postfix(NTreasureRoomRelicCollection __instance)
    {
        var holdersInUse = Traverse.Create(__instance)
            .Field<List<NTreasureRoomRelicHolder>>("_holdersInUse")
            .Value;
        if (holdersInUse == null || holdersInUse.Count <= 4)
            return;

        var anchors = holdersInUse.Take(4).ToList();
        var minX = anchors.Min(holder => holder.Position.X);
        var maxX = anchors.Max(holder => holder.Position.X);
        var centerX = (minX + maxX) * 0.5f;
        var topY = anchors.Min(holder => holder.Position.Y);
        var bottomY = anchors.Max(holder => holder.Position.Y);
        var spacing = holdersInUse.Count >= 8
            ? Math.Max(180f, (maxX - minX) / 3f)
            : Math.Max(220f, (maxX - minX) / 2f);
        var rowCounts = GetBalancedRowCounts(holdersInUse.Count);
        var consumed = 0;

        for (var rowIndex = 0; rowIndex < rowCounts.Count; rowIndex++)
        {
            var rowCount = rowCounts[rowIndex];
            if (rowCount <= 0)
                continue;

            var y = GetRowY(topY, bottomY, rowCounts.Count, rowIndex);
            LayoutHolderRow(holdersInUse, consumed, rowCount, y, centerX, spacing);
            consumed += rowCount;
        }
    }

    private static void LayoutHolderRow(
        IReadOnlyList<NTreasureRoomRelicHolder> holders,
        int startIndex,
        int count,
        float y,
        float centerX,
        float spacing)
    {
        if (count <= 0)
            return;

        var totalWidth = (count - 1) * spacing;
        var startX = centerX - totalWidth * 0.5f;
        for (var index = 0; index < count; index++)
            holders[startIndex + index].Position = new Vector2(startX + index * spacing, y);
    }

    private static List<int> GetBalancedRowCounts(int totalCount)
    {
        if (totalCount <= 0)
            return [];

        if (totalCount <= 4)
            return [totalCount];

        if (totalCount <= 8)
        {
            var topCount = (int)Math.Ceiling(totalCount / 2f);
            return [topCount, totalCount - topCount];
        }

        var rowCount = (int)Math.Ceiling(totalCount / 4f);
        var baseCount = totalCount / rowCount;
        var extra = totalCount % rowCount;
        var counts = new List<int>(rowCount);
        for (var i = 0; i < rowCount; i++)
            counts.Add(baseCount + (i < extra ? 1 : 0));

        return counts;
    }

    private static float GetRowY(float topY, float bottomY, int rowCount, int rowIndex)
    {
        if (rowCount <= 1)
            return (topY + bottomY) * 0.5f;
        if (rowCount == 2)
            return rowIndex == 0 ? topY : bottomY;

        var progress = rowIndex / (float)(rowCount - 1);
        return Mathf.Lerp(topY, bottomY, progress);
    }
}

[HarmonyPatch(typeof(NTreasureRoomRelicCollection), nameof(NTreasureRoomRelicCollection.DefaultFocusedControl),
    MethodType.Getter)]
internal static class NTreasureRoomRelicCollectionDefaultFocusExpandedPlayerPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NTreasureRoomRelicCollection __instance, ref Control? __result)
    {
        var holdersInUse = Traverse.Create(__instance)
            .Field<List<NTreasureRoomRelicHolder>>("_holdersInUse")
            .Value;
        if (holdersInUse == null || holdersInUse.Count == 0)
        {
            __result = null;
            return false;
        }

        var runState = Traverse.Create(__instance)
            .Field<IRunState>("_runState")
            .Value;
        var slotIndex = 0;
        if (runState != null)
        {
            var me = LocalContext.GetMe(runState.Players);
            if (me != null)
                slotIndex = runState.GetPlayerSlotIndex(me);
        }

        slotIndex = Math.Clamp(slotIndex, 0, holdersInUse.Count - 1);
        __result = holdersInUse[slotIndex];
        return false;
    }
}
