using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Events;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class DreamEndlessMouthOfDestructionOptionUiPatch
{
    public static void Postfix(NEventOptionButton __instance)
    {
        if (__instance.Event is not DreamEndlessMouthOfDestruction eventModel)
            return;

        if (!DreamEndlessMouthOfDestruction.TryBuildInfoOptionText(eventModel, __instance.Option?.TextKey, out var text))
            return;

        var label = __instance.GetNodeOrNull<Control>("%Text");
        if (label == null)
        {
            MainFile.Logger.Warn("[DreamEndlessMouthOfDestructionOptionUiPatch] %Text node was not found.");
            return;
        }

        __instance.MouseFilter = Control.MouseFilterEnum.Ignore;
        __instance.FocusMode = Control.FocusModeEnum.None;
        __instance.TooltipText = string.Empty;
        label.Set("text", text);
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnFocus")]
internal sealed class DreamEndlessMouthOfDestructionInfoOptionFocusPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventOptionButton __instance)
    {
        if (__instance.Event is not DreamEndlessMouthOfDestruction)
            return true;

        if (!DreamEndlessMouthOfDestruction.IsInfoTextKey(__instance.Option?.TextKey))
            return true;

        NHoverTipSet.Remove(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnUnfocus")]
internal sealed class DreamEndlessMouthOfDestructionInfoOptionUnfocusPatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventOptionButton __instance)
    {
        if (__instance.Event is not DreamEndlessMouthOfDestruction)
            return true;

        if (!DreamEndlessMouthOfDestruction.IsInfoTextKey(__instance.Option?.TextKey))
            return true;

        NHoverTipSet.Remove(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnRelease")]
internal sealed class DreamEndlessMouthOfDestructionInfoOptionReleasePatch
{
    [HarmonyPrefix]
    public static bool Prefix(NEventOptionButton __instance)
    {
        if (__instance.Event is not DreamEndlessMouthOfDestruction)
            return true;

        return !DreamEndlessMouthOfDestruction.IsInfoTextKey(__instance.Option?.TextKey);
    }
}

[HarmonyPatch(typeof(NEventRoom), "OptionButtonClicked")]
internal sealed class DreamEndlessMouthOfDestructionInfoOptionEventRoomGuardPatch
{
    [HarmonyPrefix]
    public static bool Prefix(EventOption option)
    {
        return !DreamEndlessMouthOfDestruction.IsInfoTextKey(option?.TextKey);
    }
}
