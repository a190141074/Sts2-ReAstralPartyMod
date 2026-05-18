using Godot;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed class AstralTelemetryConsentPatch : IPatchMethod
{
    private static bool _consentPromptTriggered;

    public static string PatchId => "astral_telemetry_consent_patch";
    public static bool IsCritical => false;
    public static string Description => "Show Astral telemetry consent prompt on first main-menu entry";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(NMainMenu), nameof(NMainMenu._Ready))];
    }

    public static void Postfix()
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
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}][Telemetry] NModalContainer unavailable; telemetry consent popup skipped.");
            return;
        }

        var popup = AstralTelemetryConsentPopup.Create();
        if (popup == null)
            return;

        NModalContainer.Instance.Add(popup, true);
        MainFile.Logger.Info($"[{MainFile.ModId}][Telemetry] Consent popup shown.");
    }
}
