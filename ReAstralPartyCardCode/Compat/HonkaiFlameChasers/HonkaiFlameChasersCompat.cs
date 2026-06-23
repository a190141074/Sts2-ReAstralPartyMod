using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.HonkaiFlameChasers;

internal static class HonkaiFlameChasersCompat
{
    private const string ModId = "HonkaiFlameChasers";

    public static bool IsLoaded()
    {
        return OptionalModCompatRegistry.IsModLoaded(ModId);
    }

    public static bool ShouldUseStartingPersonaFallback(RunState runState, out string reason)
    {
        if (!IsLoaded())
        {
            reason = "hfc_not_loaded";
            return false;
        }

        if (!StartingPersonRelicSelectionPatch.ShouldOpenStartingPersonaRelicSelection(runState, out var gateReason))
        {
            reason = gateReason;
            return false;
        }

        var runKey = StartingPersonRelicSelectionPatch.GetRunKey(runState);
        if (StartingPersonNeowReadyFlow.WasReadyPageInjected(runKey))
        {
            reason = "ready_page_already_injected";
            return false;
        }

        reason = "ready_page_missing_under_hfc";
        return true;
    }
}
