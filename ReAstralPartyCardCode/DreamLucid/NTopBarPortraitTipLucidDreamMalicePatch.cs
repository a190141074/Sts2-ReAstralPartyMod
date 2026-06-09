using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.sts2.Core.Nodes.TopBar;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

[HarmonyPatch(typeof(NTopBarPortraitTip), "OnFocus")]
public static class NTopBarPortraitTipLucidDreamMalicePatch
{
    public static void Postfix(NTopBarPortraitTip __instance)
    {
        var runState = GetCurrentRunState();
        if (runState == null)
            return;

        var modifier = LucidDreamMaliceModifier.Get(runState);
        if (modifier == null || !modifier.HasAnyEnabled)
            return;

        NHoverTipSet.Remove(__instance);

        var localPlayer = LocalContext.GetMe(runState);
        if (localPlayer?.Character == null)
            return;

        var ascensionTip = AscensionHelper.GetHoverTip(
            localPlayer.Character,
            runState.AscensionLevel,
            runState.GameMode.AreAchievementsAndEpochsLocked());
        var lucidDreamTip = LucidDreamMaliceUiHelper.BuildHoverTip(runState);
        if (lucidDreamTip == null)
            return;

        var tipSet = NHoverTipSet.CreateAndShow(__instance, [ascensionTip, lucidDreamTip.Value]);
        tipSet.GlobalPosition = __instance.GlobalPosition + new Vector2(0f, __instance.Size.Y + 20f);
    }

    private static RunState? GetCurrentRunState()
    {
        return AccessTools.Property(typeof(RunManager), "State")?.GetValue(RunManager.Instance) as RunState
               ?? RunManager.Instance?.DebugOnlyGetState() as RunState;
    }
}

[HarmonyPatch(typeof(NTopBarModifier), nameof(NTopBarModifier._Ready))]
public static class NTopBarModifierLucidDreamMalicePatch
{
    public static void Postfix(NTopBarModifier __instance)
    {
        var modifierField = AccessTools.Field(typeof(NTopBarModifier), "_modifier");
        var hoverTipField = AccessTools.Field(typeof(NTopBarModifier), "_hoverTip");
        if (modifierField?.GetValue(__instance) is not LucidDreamMaliceModifier modifier || hoverTipField == null)
            return;

        var hoverTip = LucidDreamMaliceUiHelper.BuildHoverTip(
            modifier.EnableFalseLifeline,
            modifier.EnableSmoothSailing,
            modifier.EnableFishScalesMalice,
            modifier.EnableSevereWoundOneMalice,
            modifier.EnableSevereWoundTwoMalice,
            modifier.EnableMadLifeMalice,
            modifier.EnableSwampOfFateMalice,
            modifier.EnableOverpopulationMalice,
            modifier.EnableCautiousJellyfishMalice);
        if (hoverTip == null)
            return;

        hoverTipField.SetValue(__instance, hoverTip.Value);
    }
}
