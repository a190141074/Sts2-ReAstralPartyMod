using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed partial class SunsetGlowElementSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private readonly TaskCompletionSource<SunsetGlowElementSelectionResult?> _completionSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _overlayClosedSource =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly IReadOnlyList<SunsetGlowElementSelectionOption> _options;
    private readonly string _title;
    private readonly string _subtitle;
    private bool _closed;
    private bool _built;
    private bool _choiceLocked;
    private VBoxContainer _root = null!;
    private Label _titleLabel = null!;
    private MegaRichTextLabel _subtitleLabel = null!;
    private Control _contentRoot = null!;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _contentRoot.GetChildren().OfType<Button>().FirstOrDefault();

    private SunsetGlowElementSelectionScreen(
        Player owner,
        IReadOnlyList<SunsetGlowElementSelectionOption> options,
        string title,
        string subtitle)
    {
        _options = options;
        _title = title;
        _subtitle = subtitle;

        Name = nameof(SunsetGlowElementSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        BuildStaticUi();
    }

    public static SunsetGlowElementSelectionScreen Create(
        Player owner,
        IReadOnlyList<SunsetGlowElementSelectionOption> options,
        string title,
        string subtitle)
    {
        return new SunsetGlowElementSelectionScreen(owner, options, title, subtitle);
    }

    public Task<SunsetGlowElementSelectionResult?> WaitForResult()
    {
        return _completionSource.Task;
    }

    public Task WaitUntilClosedAsync()
    {
        return _overlayClosedSource.Task;
    }

    public void Close()
    {
        if (_closed)
            return;

        _closed = true;
        NOverlayStack.Instance?.Remove(this);
    }

    public void AfterOverlayOpened()
    {
        Visible = true;
        if (_built)
            return;

        _built = true;
        BuildOptionCards();
    }

    public void AfterOverlayClosed()
    {
        EnsureCompletedOnForcedClose();
        _closed = true;
        _overlayClosedSource.TrySetResult();
        QueueFree();
    }

    public void AfterOverlayShown()
    {
        Visible = true;
    }

    public void AfterOverlayHidden()
    {
        Visible = false;
    }

    private void BuildStaticUi()
    {
        var shade = new ColorRect
        {
            Color = new Color(0.02f, 0.03f, 0.06f, 0.78f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);

        _root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _root.OffsetTop = 70f;
        _root.OffsetBottom = -60f;
        _root.AddThemeConstantOverride("separation", 16);
        AddChild(_root);

        _titleLabel = new Label
        {
            Text = _title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _titleLabel.AddThemeFontSizeOverride("font_size", 42);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.98f, 0.92f, 0.8f));
        _titleLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
        _titleLabel.AddThemeConstantOverride("shadow_offset_x", 1);
        _titleLabel.AddThemeConstantOverride("shadow_offset_y", 2);
        _root.AddChild(_titleLabel);

        _subtitleLabel = new MegaRichTextLabel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            BbcodeEnabled = true,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(880f, 56f),
            MaxFontSize = 26,
            MinFontSize = 20
        };
        ApplyDefaultMegaRichTextTheme(_subtitleLabel);
        _subtitleLabel.AddThemeColorOverride("default_color", new Color(0.96f, 0.96f, 0.96f));
        _subtitleLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_x", 0);
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_y", 0);
        _subtitleLabel.SetTextAutoSize(_subtitle);
        _root.AddChild(_subtitleLabel);

        _contentRoot = new Control
        {
            MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = new Vector2(1180f, 470f)
        };
        _root.AddChild(_contentRoot);
    }

    private void BuildOptionCards()
    {
        foreach (var child in _contentRoot.GetChildren())
            child.QueueFree();

        var cards = new List<Button>(_options.Count);
        for (var index = 0; index < _options.Count; index++)
        {
            var option = _options[index];
            var card = CreateOptionCard(option, index);
            _contentRoot.AddChild(card);
            cards.Add(card);
        }

        ApplyCenteredCardLayout(cards, 330f, 430f, 360f);
        ConfigureFocus(cards);
    }

    private Button CreateOptionCard(SunsetGlowElementSelectionOption option, int index)
    {
        var button = new Button
        {
            Name = $"SunsetGlowElement_{index}",
            CustomMinimumSize = new Vector2(330f, 430f),
            Flat = true,
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop
        };
        ApplyTransparentButton(button);

        var frame = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        frame.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        frame.AddThemeStyleboxOverride("panel", CreateFrameStyle(option.BorderColor, false));
        button.AddChild(frame);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 22f;
        root.OffsetTop = 28f;
        root.OffsetRight = -22f;
        root.OffsetBottom = -22f;
        root.AddThemeConstantOverride("separation", 12);
        button.AddChild(root);

        var title = new Label
        {
            Text = option.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        title.AddThemeColorOverride("font_color", option.BorderColor.Lightened(0.25f));
        title.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
        title.AddThemeConstantOverride("shadow_offset_x", 1);
        title.AddThemeConstantOverride("shadow_offset_y", 2);
        root.AddChild(title);

        var iconHolder = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(180f, 180f)
        };
        root.AddChild(iconHolder);

        var iconFrame = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        iconFrame.AddThemeStyleboxOverride("panel", CreateIconFrameStyle(option.BorderColor, false));
        iconHolder.AddChild(iconFrame);

        var icon = new TextureRect
        {
            Texture = GD.Load<Texture2D>(option.IconPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(138f, 138f)
        };
        icon.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        icon.OffsetLeft = -69f;
        icon.OffsetTop = -69f;
        icon.OffsetRight = 69f;
        icon.OffsetBottom = 69f;
        iconFrame.AddChild(icon);

        var descriptionPanel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        descriptionPanel.AddThemeStyleboxOverride("panel", CreateDescriptionPanelStyle(false));
        root.AddChild(descriptionPanel);

        var description = new MegaRichTextLabel
        {
            BbcodeEnabled = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            MaxFontSize = 20,
            MinFontSize = 16,
            CustomMinimumSize = new Vector2(250f, 132f)
        };
        ApplyDefaultMegaRichTextTheme(description);
        description.AddThemeColorOverride("default_color", new Color(0.95f, 0.95f, 0.95f));
        description.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        description.AddThemeConstantOverride("shadow_offset_x", 0);
        description.AddThemeConstantOverride("shadow_offset_y", 0);
        description.SetTextAutoSize(option.Description);
        descriptionPanel.AddChild(description);

        button.Pressed += () => Complete(option, index);
        button.MouseEntered += () => SetHoverState(button, frame, iconFrame, descriptionPanel, true, option.BorderColor);
        button.MouseExited += () => SetHoverState(button, frame, iconFrame, descriptionPanel, false, option.BorderColor);
        return button;
    }

    private void Complete(SunsetGlowElementSelectionOption option, int index)
    {
        if (_choiceLocked)
            return;

        _choiceLocked = true;
        _completionSource.TrySetResult(new SunsetGlowElementSelectionResult(option.BranchId, index, _options));
        Close();
    }

    private void EnsureCompletedOnForcedClose()
    {
        if (_completionSource.Task.IsCompleted)
            return;

        _completionSource.TrySetResult(null);
    }

    private static void ApplyDefaultMegaRichTextTheme(MegaRichTextLabel label)
    {
        var font = label.GetThemeDefaultFont();
        if (font != null)
        {
            label.AddThemeFontOverride("normal_font", font);
            label.AddThemeFontOverride("bold_font", font);
            label.AddThemeFontOverride("italics_font", font);
            label.AddThemeFontOverride("bold_italics_font", font);
            label.AddThemeFontOverride("mono_font", font);
        }

        var fontSize = label.GetThemeDefaultFontSize();
        if (fontSize <= 0)
            return;

        label.AddThemeFontSizeOverride("normal_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_font_size", fontSize);
        label.AddThemeFontSizeOverride("italics_font_size", fontSize);
        label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
        label.AddThemeFontSizeOverride("mono_font_size", fontSize);
    }

    private static void ApplyTransparentButton(Button button)
    {
        var empty = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("hover", empty);
        button.AddThemeStyleboxOverride("pressed", empty);
        button.AddThemeStyleboxOverride("focus", empty);
    }

    private static void ApplyCenteredCardLayout(IReadOnlyList<Button> cards, float width, float height, float spacing)
    {
        for (var index = 0; index < cards.Count; index++)
        {
            var x = (index - (cards.Count - 1) * 0.5f) * spacing - width * 0.5f;
            var y = -height * 0.5f;
            var card = cards[index];
            card.AnchorLeft = 0.5f;
            card.AnchorRight = 0.5f;
            card.AnchorTop = 0.5f;
            card.AnchorBottom = 0.5f;
            card.OffsetLeft = x;
            card.OffsetTop = y;
            card.OffsetRight = x + width;
            card.OffsetBottom = y + height;
            card.PivotOffset = new Vector2(width * 0.5f, height * 0.5f);
        }
    }

    private static void ConfigureFocus(IReadOnlyList<Button> buttons)
    {
        if (buttons.Count == 0)
            return;

        for (var index = 0; index < buttons.Count; index++)
        {
            var left = index > 0 ? index - 1 : buttons.Count - 1;
            var right = index < buttons.Count - 1 ? index + 1 : 0;
            buttons[index].FocusNeighborLeft = buttons[left].GetPath();
            buttons[index].FocusNeighborRight = buttons[right].GetPath();
            buttons[index].FocusNeighborTop = buttons[index].GetPath();
            buttons[index].FocusNeighborBottom = buttons[index].GetPath();
        }
    }

    private static void SetHoverState(
        Button button,
        PanelContainer frame,
        PanelContainer iconFrame,
        PanelContainer descriptionPanel,
        bool active,
        Color borderColor)
    {
        frame.AddThemeStyleboxOverride("panel", CreateFrameStyle(borderColor, active));
        iconFrame.AddThemeStyleboxOverride("panel", CreateIconFrameStyle(borderColor, active));
        descriptionPanel.AddThemeStyleboxOverride("panel", CreateDescriptionPanelStyle(active));
        button.Scale = active ? Vector2.One * 1.02f : Vector2.One;
    }

    private static StyleBoxFlat CreateFrameStyle(Color borderColor, bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? new Color(0.09f, 0.11f, 0.14f, 0.16f) : new Color(0.05f, 0.07f, 0.10f, 0.10f),
            BorderColor = borderColor,
            ShadowColor = new Color(0f, 0f, 0f, active ? 0.28f : 0.18f),
            ShadowSize = active ? 12 : 8,
            ShadowOffset = new Vector2(0f, active ? 7f : 4f)
        };
        style.SetBorderWidthAll(active ? 4 : 3);
        style.SetCornerRadiusAll(24);
        return style;
    }

    private static StyleBoxFlat CreateIconFrameStyle(Color borderColor, bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.08f, 0.1f, 0.12f, active ? 0.94f : 0.88f),
            BorderColor = borderColor.Lightened(active ? 0.16f : 0.05f)
        };
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(22);
        style.ContentMarginLeft = 18f;
        style.ContentMarginRight = 18f;
        style.ContentMarginTop = 18f;
        style.ContentMarginBottom = 18f;
        return style;
    }

    private static StyleBoxFlat CreateDescriptionPanelStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? new Color(0f, 0f, 0f, 0.24f) : new Color(0f, 0f, 0f, 0.16f)
        };
        style.SetCornerRadiusAll(14);
        style.ContentMarginLeft = 14f;
        style.ContentMarginRight = 14f;
        style.ContentMarginTop = 10f;
        style.ContentMarginBottom = 10f;
        return style;
    }
}
