using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

[HarmonyPatch(typeof(RunHistoryUtilities), nameof(RunHistoryUtilities.CreateRunHistoryEntry))]
public static class RunHistoryLucidDreamModifierFilterPatch
{
    public static void Prefix(SerializableRun run, bool victory, bool isAbandoned, PlatformType platformType)
    {
        if (run?.Modifiers == null || run.Modifiers.Count == 0)
            return;

        var filteredModifiers = run.Modifiers
            .Where(static modifier => modifier.Id != ModelDb.Modifier<LucidDreamMaliceModifier>().Id)
            .ToList();

        if (filteredModifiers.Count == run.Modifiers.Count)
            return;

        run.Modifiers = filteredModifiers;
    }
}
