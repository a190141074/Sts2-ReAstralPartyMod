using System.ComponentModel;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed partial class AstralTelemetryConsentPopup : Control, IScreenContext
{
    private const string ScenePath = "res://scenes/ui/vertical_popup.tscn";

    private NVerticalPopup? _verticalPopup;

    public Control? DefaultFocusedControl => null;

    public static AstralTelemetryConsentPopup? Create()
    {
        var scene = GD.Load<PackedScene>(ScenePath);
        if (scene == null)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to load consent popup scene: {ScenePath}");
            return null;
        }

        var popup = new AstralTelemetryConsentPopup();
        popup.SetAnchorsPreset(LayoutPreset.FullRect);
        popup._verticalPopup = scene.Instantiate<NVerticalPopup>(PackedScene.GenEditState.Disabled);
        if (popup._verticalPopup == null)
        {
            MainFile.Logger.Warn($"[{MainFile.ModId}][Telemetry] Failed to instantiate consent popup.");
            return null;
        }

        popup.AddChild(popup._verticalPopup);
        return popup;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public override void _Ready()
    {
        base._Ready();
        SetupContent();
    }

    private void SetupContent()
    {
        if (_verticalPopup == null)
            return;

        _verticalPopup.SetText(
            new LocString("settings_ui", "RE_ASTRAL_PARTY_TELEMETRY_POPUP.title").GetRawText(),
            new LocString("settings_ui", "RE_ASTRAL_PARTY_TELEMETRY_POPUP.body").GetRawText());
        _verticalPopup.InitYesButton(
            new LocString("settings_ui", "RE_ASTRAL_PARTY_TELEMETRY_POPUP.enable"),
            _ =>
            {
                AstralTelemetry.SetCollectionEnabledByConsent(true);
                ClosePopup();
            });
        _verticalPopup.InitNoButton(
            new LocString("settings_ui", "RE_ASTRAL_PARTY_TELEMETRY_POPUP.disable"),
            _ =>
            {
                AstralTelemetry.SetCollectionEnabledByConsent(false);
                ClosePopup();
            });
    }

    private void ClosePopup()
    {
        NModalContainer.Instance?.Clear();
        QueueFree();
    }
}
