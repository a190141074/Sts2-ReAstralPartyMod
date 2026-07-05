using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class RunHistoryModifierVisibilityFilterPatch
{
    [HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
    [HarmonyPostfix]
    public static void Postfix(SerializableRun run, bool victory, bool isAbandoned, PlatformType platformType)
    {
        if (run?.Modifiers == null || run.Modifiers.Count == 0)
            return;

        var filteredModifiers = ModifierDisplayVisibilityHelper
            .FilterVisibleModifiers(run.Modifiers)
            .ToList();

        if (filteredModifiers.Count == run.Modifiers.Count)
            return;

        run.Modifiers = filteredModifiers;
    }
}

[HarmonyPatch(typeof(NCustomRunModifiersList), "GetAllModifiers")]
internal static class CustomRunModifiersListVisibilityFilterPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref IEnumerable<ModifierModel> __result)
    {
        __result = ModifierDisplayVisibilityHelper.FilterVisibleModifiers(__result);
    }
}
