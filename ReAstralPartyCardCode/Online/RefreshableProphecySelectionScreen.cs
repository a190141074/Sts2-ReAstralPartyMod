using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed partial class RefreshableProphecySelectionScreen : Control, IOverlayScreen, IScreenContext
{

    private readonly TaskCompletionSource<RefreshableProphecySelectionResult> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _overlayClosedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Player _owner;
    private readonly Func<IReadOnlyList<ProphecySoulDevourKind>, int, IReadOnlySet<ProphecySoulDevourKind>, IReadOnlyList<ProphecySoulDevourKind>> _rerollFunc;
    private readonly List<int> _rerollHistory = [];
    private readonly HashSet<ProphecySoulDevourKind> _seenOptions = [];
    private IReadOnlyList<ProphecySoulDevourKind> _options;
    private readonly int _refreshCost;
    private readonly bool _allowRefresh;

    private VBoxContainer _rootContainer = null!;
    private VBoxContainer _optionContainer = null!;
    private Label _titleLabel = null!;
    private MegaRichTextLabel _subtitleLabel = null!;
    private Button _refreshButton = null!;
    private bool _closed;
    private bool _choiceLocked;
    private bool _built;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _optionContainer.GetChildren().OfType<Button>().FirstOrDefault();

    private RefreshableProphecySelectionScreen(
        Player owner,
        IReadOnlyList<ProphecySoulDevourKind> options,
        string title,
        string subtitle,
        int refreshCost,
        bool allowRefresh,
        Func<IReadOnlyList<ProphecySoulDevourKind>, int, IReadOnlySet<ProphecySoulDevourKind>, IReadOnlyList<ProphecySoulDevourKind>> rerollFunc)
    {
        _owner = owner;
        _options = options;
        _refreshCost = refreshCost;
        _allowRefresh = allowRefresh;
        _rerollFunc = rerollFunc;

        Name = nameof(RefreshableProphecySelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        BuildUi(title, subtitle);
        UpdateRefreshButton();
    }

    public static RefreshableProphecySelectionScreen Create(
        Player owner,
        IReadOnlyList<ProphecySoulDevourKind> options,
        string title,
        string subtitle,
        int refreshCost,
        bool allowRefresh,
        Func<IReadOnlyList<ProphecySoulDevourKind>, int, IReadOnlySet<ProphecySoulDevourKind>, IReadOnlyList<ProphecySoulDevourKind>> rerollFunc)
    {
        return new RefreshableProphecySelectionScreen(owner, options, title, subtitle, refreshCost, allowRefresh, rerollFunc);
    }

    public Task<RefreshableProphecySelectionResult> WaitForResult()
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
        BuildOptionButtonsIfNeeded();
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

    private void BuildUi(string title, string subtitle)
    {
        var backstop = new ColorRect
        {
            Color = new Color(0.04f, 0.04f, 0.06f, 0.72f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        backstop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backstop);

        _rootContainer = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        _rootContainer.AddThemeConstantOverride("separation", 14);
        _rootContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        _rootContainer.OffsetLeft = -280f;
        _rootContainer.OffsetTop = 132f;
        _rootContainer.OffsetRight = 280f;
        _rootContainer.OffsetBottom = 0f;
        AddChild(_rootContainer);

        _titleLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        ApplyHeaderTheme(_titleLabel, 38, new Color(0.98f, 0.94f, 0.82f));
        _rootContainer.AddChild(_titleLabel);

        _subtitleLabel = new MegaRichTextLabel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            BbcodeEnabled = true,
            MaxFontSize = 24,
            MinFontSize = 20,
            CustomMinimumSize = new Vector2(560f, 54f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        ApplyDefaultMegaRichTextTheme(_subtitleLabel);
        _subtitleLabel.AddThemeColorOverride("default_color", new Color(0.92f, 0.92f, 0.92f));
        _subtitleLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_x", 0);
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_y", 0);
        _subtitleLabel.SetTextAutoSize(subtitle);
        _rootContainer.AddChild(_subtitleLabel);

        _optionContainer = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _optionContainer.AddThemeConstantOverride("separation", 20);
        _rootContainer.AddChild(_optionContainer);

        _refreshButton = new Button
        {
            CustomMinimumSize = new Vector2(208f, 54f),
            Text = "刷新"
        };
        ApplyRefreshButtonTheme(_refreshButton);
        _refreshButton.Pressed += OnRefreshPressed;
        _rootContainer.AddChild(_refreshButton);
    }

    private void BuildOptionButtonsIfNeeded()
    {
        if (_built)
            return;

        _built = true;
        RebuildOptions();
    }

    private void RebuildOptions()
    {
        foreach (var child in _optionContainer.GetChildren())
            child.QueueFree();

        foreach (var option in _options)
            _seenOptions.Add(option);

        for (var i = 0; i < _options.Count; i++)
        {
            var capturedIndex = i;
            var definition = ProphecySoulDevourRegistry.Get(_options[i]);
            var optionRoot = new PanelContainer
            {
                CustomMinimumSize = new Vector2(560f, 156f),
                MouseFilter = MouseFilterEnum.Ignore
            };
            ApplyOptionPanelTheme(optionRoot);
            _optionContainer.AddChild(optionRoot);

            var cardRoot = new Control
            {
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(560f, 156f)
            };
            optionRoot.AddChild(cardRoot);

            var holderButton = new Button
            {
                CustomMinimumSize = new Vector2(560f, 156f),
                Flat = true,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All
            };
            var empty = new StyleBoxEmpty();
            holderButton.AddThemeStyleboxOverride("normal", empty);
            holderButton.AddThemeStyleboxOverride("hover", empty);
            holderButton.AddThemeStyleboxOverride("pressed", empty);
            holderButton.AddThemeStyleboxOverride("focus", empty);
            holderButton.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holderButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
            cardRoot.AddChild(holderButton);

            var titleBar = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            titleBar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
            titleBar.OffsetLeft = 18f;
            titleBar.OffsetRight = -18f;
            titleBar.OffsetTop = 12f;
            titleBar.OffsetBottom = 46f;
            ApplyOptionTitleBarTheme(titleBar);
            cardRoot.AddChild(titleBar);

            var title = new Label
            {
                Text = definition.TitleLocString.GetFormattedText(),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MouseFilter = MouseFilterEnum.Ignore
            };
            title.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            ApplyTitleTheme(title);
            titleBar.AddChild(title);

            var descriptionPanel = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            descriptionPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            descriptionPanel.OffsetLeft = 14f;
            descriptionPanel.OffsetTop = 58f;
            descriptionPanel.OffsetRight = -14f;
            descriptionPanel.OffsetBottom = -12f;
            ApplyDescriptionPanelTheme(descriptionPanel);
            cardRoot.AddChild(descriptionPanel);

            var description = new MegaRichTextLabel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                MaxFontSize = 22,
                MinFontSize = 18,
                MouseFilter = MouseFilterEnum.Ignore,
                BbcodeEnabled = true
            };
            description.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            ApplyDefaultMegaRichTextTheme(description);
            description.AddThemeColorOverride("default_color", new Color(0.97f, 0.97f, 0.97f, 0.96f));
            description.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
            description.AddThemeConstantOverride("shadow_offset_x", 0);
            description.AddThemeConstantOverride("shadow_offset_y", 0);
            description.SetTextAutoSize(definition.DescriptionLocString.GetFormattedText());
            descriptionPanel.AddChild(description);

            SetOptionVisualState(optionRoot, titleBar, descriptionPanel, cardRoot, false);
            holderButton.MouseEntered += () => SetOptionVisualState(optionRoot, titleBar, descriptionPanel, cardRoot, true);
            holderButton.MouseExited += () => SetOptionVisualState(optionRoot, titleBar, descriptionPanel, cardRoot, false);
            holderButton.Pressed += () => OnOptionSelected(capturedIndex);
        }

        UpdateRefreshButton();
    }

    private void OnRefreshPressed()
    {
        if (_choiceLocked)
            return;
        if (!_allowRefresh)
            return;
        if (_owner.Gold < _refreshCost)
            return;

        var rerollOrdinal = _rerollHistory.Count;
        var rerolled = _rerollFunc(_options, rerollOrdinal, _seenOptions);
        if (rerolled.Count == 0)
            return;

        _options = rerolled;
        _rerollHistory.Add(rerollOrdinal);
        RebuildOptions();
        UpdateRefreshButton();
    }

    private void OnOptionSelected(int index)
    {
        if (_choiceLocked || index < 0 || index >= _options.Count)
            return;

        _choiceLocked = true;
        Complete(_options[index], index);
    }

    private void Complete(ProphecySoulDevourKind selectedProphecy, int index)
    {
        _completionSource.TrySetResult(new RefreshableProphecySelectionResult
        {
            SelectedProphecy = selectedProphecy,
            SelectedIndex = index,
            RefreshCost = _refreshCost,
            RefreshCount = _rerollHistory.Count,
            RerollHistory = _rerollHistory.ToList(),
            FinalOptions = _options.ToList()
        });
        Close();
    }

    private void EnsureCompletedOnForcedClose()
    {
        if (_completionSource.Task.IsCompleted)
            return;

        var fallback = _options.FirstOrDefault();
        _completionSource.TrySetResult(new RefreshableProphecySelectionResult
        {
            SelectedProphecy = fallback,
            SelectedIndex = fallback == default ? -1 : 0,
            RefreshCost = _refreshCost,
            RefreshCount = _rerollHistory.Count,
            RerollHistory = _rerollHistory.ToList(),
            FinalOptions = _options.ToList()
        });
    }

    private void UpdateRefreshButton()
    {
        _refreshButton.Visible = _allowRefresh;
        if (!_allowRefresh)
            return;

        _refreshButton.Text = string.Format(
            ProphecySoulDevourRegistry.RefreshButtonText.GetFormattedText(),
            _refreshCost,
            _owner.Gold);
        _refreshButton.Disabled = _choiceLocked || _owner.Gold < _refreshCost;
    }

    private static void ApplyHeaderTheme(Label label, int fontSize, Color color)
    {
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
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
        if (fontSize > 0)
        {
            label.AddThemeFontSizeOverride("normal_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_font_size", fontSize);
            label.AddThemeFontSizeOverride("italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("bold_italics_font_size", fontSize);
            label.AddThemeFontSizeOverride("mono_font_size", fontSize);
        }
    }

    private static void ApplyTitleTheme(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.2f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.55f));
        label.AddThemeConstantOverride("shadow_offset_x", 1);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
    }

    private static void ApplyOptionPanelTheme(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreateOptionPanelStyle(false));
    }

    private static void ApplyOptionTitleBarTheme(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreateOptionTitleBarStyle(false));
    }

    private static void ApplyDescriptionPanelTheme(PanelContainer panel)
    {
        panel.AddThemeStyleboxOverride("panel", CreateDescriptionPanelStyle(false));
    }

    private static void SetOptionVisualState(
        PanelContainer optionRoot,
        PanelContainer titleBar,
        PanelContainer descriptionPanel,
        Control cardRoot,
        bool active)
    {
        optionRoot.AddThemeStyleboxOverride("panel", CreateOptionPanelStyle(active));
        titleBar.AddThemeStyleboxOverride("panel", CreateOptionTitleBarStyle(active));
        descriptionPanel.AddThemeStyleboxOverride("panel", CreateDescriptionPanelStyle(active));
        cardRoot.Position = active ? new Vector2(0f, -4f) : Vector2.Zero;
    }

    private static StyleBoxFlat CreateOptionPanelStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active
                ? new Color(0.14f, 0.18f, 0.2f, 0.97f)
                : new Color(27f / 255f, 33f / 255f, 33f / 255f, 0.95f),
            BorderColor = active
                ? new Color(0.6f, 0.78f, 1f, 0.96f)
                : new Color(76f / 255f, 95f / 255f, 130f / 255f, 0.84f),
            ShadowColor = new Color(0f, 0f, 0f, active ? 0.34f : 0.24f),
            ShadowSize = active ? 12 : 8,
            ShadowOffset = new Vector2(0f, active ? 6f : 4f)
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(18);
        style.ContentMarginLeft = 0f;
        style.ContentMarginTop = 0f;
        style.ContentMarginRight = 0f;
        style.ContentMarginBottom = 0f;
        return style;
    }

    private static StyleBoxFlat CreateOptionTitleBarStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active
                ? new Color(0.12f, 0.16f, 0.22f, 0.99f)
                : new Color(0.09f, 0.12f, 0.16f, 0.98f),
            BorderColor = active
                ? new Color(0.54f, 0.72f, 0.98f, 0.96f)
                : new Color(0.3f, 0.42f, 0.58f, 0.88f)
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(10);
        style.ContentMarginLeft = 12f;
        style.ContentMarginRight = 12f;
        style.ContentMarginTop = 6f;
        style.ContentMarginBottom = 6f;
        return style;
    }

    private static StyleBoxFlat CreateDescriptionPanelStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? new Color(0f, 0f, 0f, 0.22f) : new Color(0f, 0f, 0f, 0.16f)
        };
        style.SetCornerRadiusAll(14);
        style.ContentMarginLeft = 22f;
        style.ContentMarginRight = 22f;
        style.ContentMarginTop = 10f;
        style.ContentMarginBottom = 14f;
        return style;
    }

    private static void ApplyRefreshButtonTheme(Button button)
    {
        button.AddThemeFontSizeOverride("font_size", 24);
        button.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_hover_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_focus_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.16f, 0.16f, 0.16f, 0.72f));

        var normal = CreateRefreshButtonStyle(
            new Color(0.98f, 0.82f, 0.16f),
            new Color(1f, 0.9f, 0.4f));
        var hover = CreateRefreshButtonStyle(
            new Color(1f, 0.86f, 0.22f),
            new Color(1f, 0.94f, 0.5f));
        var pressed = CreateRefreshButtonStyle(
            new Color(0.92f, 0.76f, 0.1f),
            new Color(1f, 0.88f, 0.36f));
        var disabled = CreateRefreshButtonStyle(
            new Color(0.56f, 0.5f, 0.34f, 0.86f),
            new Color(0.72f, 0.66f, 0.48f, 0.92f));

        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", hover);
        button.AddThemeStyleboxOverride("disabled", disabled);
    }

    private static StyleBoxFlat CreateRefreshButtonStyle(Color background, Color border)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            ShadowColor = new Color(0f, 0f, 0f, 0.16f),
            ShadowSize = 6,
            ShadowOffset = new Vector2(0f, 3f)
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(16);
        style.ContentMarginLeft = 10f;
        style.ContentMarginRight = 10f;
        style.ContentMarginTop = 5f;
        style.ContentMarginBottom = 5f;
        return style;
    }
}
