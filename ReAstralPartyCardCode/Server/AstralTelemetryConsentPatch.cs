using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

[HarmonyPatch(typeof(NMainMenu))]
public static class AstralTelemetryConsentPatch
{
    private static bool _consentPromptTriggered;

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NMainMenu._Ready))]
    public static void AfterMainMenuReady()
    {
        if (_consentPromptTriggered)
            return;

        if (!AstralTelemetry.ShouldShowConsentPrompt())
            return;

        _consentPromptTriggered = true;
        Callable.From(ShowConsentPopup).CallDeferred();
    }

    private static void ShowConsentPopup()
    {
        if (!AstralTelemetry.ShouldShowConsentPrompt())
            return;

        if (NModalContainer.Instance == null)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] NModalContainer unavailable; telemetry consent popup skipped.");
            return;
        }

        var popup = AstralTelemetryConsentPopup.Create();
        if (popup == null)
            return;

        NModalContainer.Instance.Add(popup, showBackstop: true);
        MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Consent popup shown.");
    }
}
