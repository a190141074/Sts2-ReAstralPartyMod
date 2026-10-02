using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

// These paths do not necessarily publish RunEndedEvent (which is a run result).
[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class AstralSamuraiPrawnMusicRunCleanupPatch
{
    private static void Prefix() => AstralSamuraiPrawn.StopActiveMusic();
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.LocalPlayerDisconnected))]
internal static class AstralSamuraiPrawnMusicLocalDisconnectPatch
{
    private static void Prefix() => AstralSamuraiPrawn.StopActiveMusic();
}
