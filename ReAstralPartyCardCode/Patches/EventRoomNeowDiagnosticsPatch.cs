using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class EventRoomNeowDiagnosticsPatch
{
    public static void Postfix(object __instance)
    {
        AstralNeowDiagnosticHelper.ReportEventRoomNodeReady(__instance);
    }
}

internal static class AncientEventLayoutNeowDiagnosticsPatch
{
    public static void Postfix(object __instance)
    {
        AstralNeowDiagnosticHelper.ReportAncientLayoutReady(__instance);
    }
}
