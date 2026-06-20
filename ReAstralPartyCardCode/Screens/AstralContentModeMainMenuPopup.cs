using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Ui.Shell;
using STS2RitsuLib.Ui.Shell.Theme;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Screens;

internal static class AstralContentModeMainMenuPopup
{
    public static void Open()
    {
        try
        {
            if (NModalContainer.Instance == null)
                return;

            NModalContainer.Instance.Clear();
            NModalContainer.Instance.Add(new AstralContentModeMainMenuPanel());
            NModalContainer.Instance.ShowBackstop();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[astral_content_mode_main_menu_popup] Failed to open content mode popup: {ex}");
        }
    }
}

internal sealed partial class AstralContentModeMainMenuPanel : Control, IScreenContext
{
    private const string SettingsLocTable = "settings_ui";
    private const string LocPrefix = "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode_screen";
    private const float PopupMinWidth = 920f;
    private const float PopupMinHeight = 360f;
    private readonly List<Control> _focusChain = [];
    private readonly List<ModeButtonView> _modeButtons = [];
    private bool _focusRefreshScheduled;

    public Control? DefaultFocusedControl { get; private set; }

    private sealed class ModeButtonView
    {
        public required AstralContentMode Mode { get; init; }
        public required PanelContainer Card { get; init; }
        public required ModSettingsTextButton Button { get; init; }
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        SetProcessUnhandledInput(true);
        Build();
        RefreshCards();
        ScheduleFocusRefresh();
        Callable.From(() =>
        {
            DefaultFocusedControl ??= _focusChain.FirstOrDefault();
            DefaultFocusedControl?.GrabFocus();
        }).CallDeferred();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsEcho() &&
            (@event.IsActionPressed(MegaInput.cancel) || @event.IsActionPressed(MegaInput.pauseAndBack)))
        {
            Close();
            GetViewport()?.SetInputAsHandled();
            return;
        }

        base._UnhandledInput(@event);
    }

    private void Build()
    {
        var backdrop = new ColorRect
        {
            Color = RitsuShellTheme.Current.Color.ModalBackdrop,
            MouseFilter = MouseFilterEnum.Ignore
        };
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(PopupMinWidth, PopupMinHeight),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        panel.AddThemeStyleboxOverride("panel",
            RitsuShellPanelStyles.CreateFramedSurface(
                RitsuShellTheme.Current.Surface.Content,
                RitsuShellTheme.Current.Metric.Radius.Default));
        center.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 22);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 22);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        panel.AddChild(margin);

        var root = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 18);
        margin.AddChild(root);

        root.AddChild(BuildHeader());
        root.AddChild(BuildBody());
    }

    private Control BuildHeader()
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddThemeConstantOverride("separation", 16);

        var title = CreateLabel(Loc($"{LocPrefix}.title"), 28, RitsuShellTheme.Current.Text.RichTitle, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(title);

        var close = new ModSettingsTextButton(
            Loc($"{LocPrefix}.close"),
            ModSettingsButtonTone.Normal,
            Close)
        {
            CustomMinimumSize = new Vector2(150f, RitsuShellTheme.Current.Metric.Entry.ValueMinHeight)
        };
        row.AddChild(close);
        DefaultFocusedControl = close;
        return row;
    }

    private Control BuildBody()
    {
        var center = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 20);
        center.AddChild(row);

        foreach (var definition in AstralContentModeRegistry.Modes)
        {
            var card = BuildModeCard(definition.Mode, definition.TitleLocKey);
            _modeButtons.Add(card);
            row.AddChild(card.Card);
        }

        return center;
    }

    private ModeButtonView BuildModeCard(AstralContentMode mode, string titleKey)
    {
        var card = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(320f, 156f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };

        var margin = new MarginContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        card.AddChild(margin);

        var content = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        content.AddThemeConstantOverride("separation", 10);
        margin.AddChild(content);

        var title = CreateLabel(Loc(titleKey), 28, RitsuShellTheme.Current.Text.RichTitle, true);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.VerticalAlignment = VerticalAlignment.Center;
        title.CustomMinimumSize = new Vector2(0f, 44f);
        content.AddChild(title);

        var button = new ModSettingsTextButton(
            Loc(titleKey),
            ModSettingsButtonTone.Normal,
            () => OnModeSelected(mode))
        {
            CustomMinimumSize = new Vector2(236f, 52f),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        content.AddChild(button);

        return new ModeButtonView
        {
            Mode = mode,
            Card = card,
            Button = button
        };
    }

    private void OnModeSelected(AstralContentMode mode)
    {
        ReAstralPartyModSettingsManager.SetCurrentContentMode(mode);
        LobbyGameplaySettingsSync.ResetForContentModeSwitch();
        CharacterSelectGameplayPreviewPanelRuntimeBridge.RequestRefreshAll();
        RefreshCards();
        ScheduleFocusRefresh();
    }

    private void RefreshCards()
    {
        var currentMode = ReAstralPartyModSettingsManager.GetCurrentContentMode();
        foreach (var modeButton in _modeButtons)
        {
            var active = modeButton.Mode == currentMode;
            modeButton.Card.AddThemeStyleboxOverride(
                "panel",
                CreateModeCardStyle(active));
            modeButton.Button.SetSelected(active);
        }
    }

    private static StyleBoxFlat CreateModeCardStyle(bool active)
    {
        var style = RitsuShellPanelStyles.CreateFramedSurface(
            active ? RitsuShellTheme.Current.Surface.Sidebar : RitsuShellTheme.Current.Surface.Content,
            RitsuShellTheme.Current.Metric.Radius.Default);
        style.BorderColor = active
            ? RitsuShellTheme.Current.Text.HoverHighlight
            : RitsuShellTheme.Current.Surface.Framed.Border;
        style.BorderWidthLeft = 2;
        style.BorderWidthTop = 2;
        style.BorderWidthRight = 2;
        style.BorderWidthBottom = 2;
        style.ShadowSize = active ? 14 : 10;
        return style;
    }

    private void Close()
    {
        NModalContainer.Instance?.Clear();
    }

    private void ScheduleFocusRefresh()
    {
        if (_focusRefreshScheduled)
            return;

        _focusRefreshScheduled = true;
        Callable.From(RefreshFocusNavigationDeferred).CallDeferred();
    }

    private void RefreshFocusNavigationDeferred()
    {
        _focusRefreshScheduled = false;
        if (!IsInsideTree())
            return;

        _focusChain.Clear();
        CollectFocusChain(this, _focusChain);
        WireFocusChain(_focusChain);

        var owner = GetViewport()?.GuiGetFocusOwner();
        if (owner != null && IsAncestorOf(owner) && owner.IsVisibleInTree())
            return;

        DefaultFocusedControl ??= _focusChain.FirstOrDefault();
        DefaultFocusedControl?.GrabFocus();
    }

    private static void CollectFocusChain(Control root, ICollection<Control> chain)
    {
        if (root.IsVisibleInTree() &&
            root.FocusMode == FocusModeEnum.All &&
            root is BaseButton)
            chain.Add(root);

        foreach (var child in root.GetChildren())
        {
            if (child is not Control control || !control.IsVisibleInTree())
                continue;

            CollectFocusChain(control, chain);
        }
    }

    private static void WireFocusChain(IReadOnlyList<Control> chain)
    {
        for (var i = 0; i < chain.Count; i++)
        {
            var current = chain[i];
            var self = current.GetPath();
            current.FocusNeighborLeft = i > 0 ? chain[i - 1].GetPath() : self;
            current.FocusNeighborRight = i < chain.Count - 1 ? chain[i + 1].GetPath() : self;
            current.FocusNeighborTop = self;
            current.FocusNeighborBottom = self;
        }
    }

    private static Label CreateLabel(string text, int fontSize, Color color, bool bold = false)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        }.Also(label =>
        {
            label.AddThemeFontOverride("font",
                bold ? RitsuShellTheme.Current.Font.BodyBold : RitsuShellTheme.Current.Font.Body);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
        });
    }

    private static string Loc(string key)
    {
        return new LocString(SettingsLocTable, key).GetRawText();
    }
}

internal static class AstralContentModeMainMenuPanelExtensions
{
    public static T Also<T>(this T value, Action<T> action)
    {
        action(value);
        return value;
    }
}
