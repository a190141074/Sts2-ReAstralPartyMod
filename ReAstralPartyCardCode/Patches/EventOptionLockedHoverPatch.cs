using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NEventOptionButton), "OnFocus")]
public static class EventOptionLockedHoverFocusPatch
{
    public static bool Prefix(NEventOptionButton __instance)
    {
        if (__instance.Option is not { IsLocked: true })
            return true;

        if (__instance.Option.HoverTips == null)
            return false;

        NHoverTipSet.CreateAndShow(
            __instance,
            __instance.Option.HoverTips,
            (__instance.Event.LayoutType != MegaCrit.Sts2.Core.Events.EventLayoutType.Combat)
                ? HoverTipAlignment.Left
                : HoverTipAlignment.Right);
        return false;
    }
}

[HarmonyPatch(typeof(NEventOptionButton), "OnUnfocus")]
public static class EventOptionLockedHoverUnfocusPatch
{
    public static bool Prefix(NEventOptionButton __instance)
    {
        if (__instance.Option is not { IsLocked: true })
            return true;

        NHoverTipSet.Remove(__instance);
        return false;
    }
}
