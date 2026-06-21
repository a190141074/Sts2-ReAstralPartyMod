using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed partial class GroupedRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private const float HolderWidth = 136f;
    private const float HolderHeight = 136f;
    private const float GroupCardWidth = 248f;
    private const float GroupCardHeight = 330f;
    private const float GroupIconSize = 148f;
    private const float RelicIconSize = 120f;
    private const string GroupBackgroundPath = "res://ReAstralPartyMod/images/background/starting_person_pelic_selection_screen.png";
    private const string CommonRelicBasePath = "res://ReAstralPartyMod/images/ui/relic_base_blue.png";
    private const string UncommonRelicBasePath = "res://ReAstralPartyMod/images/ui/relic_base_purple.png";
    private const string RareRelicBasePath = "res://ReAstralPartyMod/images/ui/relic_base_gold.png";
    private const string OptionBackgroundPath = "res://ReAstralPartyMod/images/ui/relic_background.png";

    private readonly TaskCompletionSource<RelicModel?> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _overlayClosedSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly Player _owner;
    private readonly IReadOnlyList<RelicModel> _allOptions;
    private readonly IReadOnlyList<AncientRelicGroupOption> _groupOptions;
    private readonly bool _allowSkip;
    private readonly string _title;
    private readonly string _subtitle;
    private readonly string _groupTitle;
    private readonly string _groupSubtitle;
    private readonly string _relicTitle;
    private readonly string _relicSubtitle;
    private readonly string _skipText;

    private readonly Dictionary<int, Button> _buttonsByIndex = [];
    private readonly Dictionary<int, TextureRect> _texturesByIndex = [];
    private readonly Stack<AncientRelicGroupOption> _navigationStack = [];

    private Label _titleLabel = null!;
    private MegaRichTextLabel _subtitleLabel = null!;
    private Control _contentRoot = null!;
    private Control _groupContent = null!;
    private Control _relicContent = null!;
    private Button _backButton = null!;
    private Button _skipButton = null!;
    private bool _closed;
    private bool _built;
    private bool _choiceLocked;
    private AncientRelicGroupOption? _currentGroup;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _buttonsByIndex.OrderBy(pair => pair.Key).Select(pair => pair.Value).FirstOrDefault();

    private GroupedRelicSelectionScreen(
        Player owner,
        IReadOnlyList<RelicModel> allOptions,
        IReadOnlyList<AncientRelicGroupOption> groupOptions,
        string title,
        string subtitle,
        string groupTitle,
        string groupSubtitle,
        string relicTitle,
        string relicSubtitle,
        string skipText,
        bool allowSkip)
    {
        _owner = owner;
        _allOptions = allOptions;
        _groupOptions = groupOptions;
        _title = title;
        _subtitle = subtitle;
        _groupTitle = groupTitle;
        _groupSubtitle = groupSubtitle;
        _relicTitle = relicTitle;
        _relicSubtitle = relicSubtitle;
        _skipText = skipText;
        _allowSkip = allowSkip;

        Name = nameof(GroupedRelicSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        BuildStaticUi();
    }

    public static GroupedRelicSelectionScreen Create(
        Player owner,
        IReadOnlyList<RelicModel> allOptions,
        IReadOnlyList<AncientRelicGroupOption> groupOptions,
        string title,
        string subtitle,
        string groupTitle,
        string groupSubtitle,
        string relicTitle,
        string relicSubtitle,
        string skipText,
        bool allowSkip)
    {
        return new GroupedRelicSelectionScreen(
            owner,
            allOptions,
            groupOptions,
            title,
            subtitle,
            groupTitle,
            groupSubtitle,
            relicTitle,
            relicSubtitle,
            skipText,
            allowSkip);
    }

    public Task<RelicModel?> WaitForResult()
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
        ShowGroupStep();
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
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>(GroupBackgroundPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(background);

        var shade = new ColorRect
        {
            Color = new Color(0.02f, 0.02f, 0.04f, 0.46f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(shade);

        var titlePanel = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titlePanel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        titlePanel.OffsetTop = 88f;
        titlePanel.OffsetLeft = 90f;
        titlePanel.OffsetRight = -90f;
        titlePanel.AddThemeConstantOverride("separation", 8);
        AddChild(titlePanel);

        _titleLabel = new Label
        {
            Text = _title,
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderLarge"
        };
        titlePanel.AddChild(_titleLabel);

        _subtitleLabel = new MegaRichTextLabel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            BbcodeEnabled = true,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(760f, 54f),
            MaxFontSize = 26,
            MinFontSize = 20
        };
        ApplyDefaultMegaRichTextTheme(_subtitleLabel);
        _subtitleLabel.AddThemeColorOverride("default_color", new Color(0.95f, 0.95f, 0.95f));
        _subtitleLabel.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_x", 0);
        _subtitleLabel.AddThemeConstantOverride("shadow_offset_y", 0);
        titlePanel.AddChild(_subtitleLabel);

        _contentRoot = new Control
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        _contentRoot.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentRoot.OffsetTop = 200f;
        _contentRoot.OffsetBottom = -130f;
        AddChild(_contentRoot);

        _groupContent = new Control
        {
            Name = "GroupContent",
            MouseFilter = MouseFilterEnum.Pass
        };
        _groupContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentRoot.AddChild(_groupContent);

        _relicContent = new Control
        {
            Name = "RelicContent",
            MouseFilter = MouseFilterEnum.Pass,
            Visible = false
        };
        _relicContent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _contentRoot.AddChild(_relicContent);

        _backButton = CreateBottomButton("返回");
        _backButton.OffsetLeft = -320f;
        _backButton.OffsetRight = -120f;
        _backButton.Pressed += OnBackPressed;
        _backButton.Visible = false;
        AddChild(_backButton);

        _skipButton = CreateBottomButton(_skipText);
        _skipButton.OffsetLeft = 120f;
        _skipButton.OffsetRight = 320f;
        _skipButton.Pressed += OnSkipPressed;
        _skipButton.Visible = _allowSkip;
        AddChild(_skipButton);
    }

    private void ShowGroupStep()
    {
        _currentGroup = null;
        _groupContent.Visible = true;
        _relicContent.Visible = false;
        _backButton.Visible = false;
        _titleLabel.Text = _groupTitle;
        _subtitleLabel.SetTextAutoSize(_groupSubtitle);
        ClearButtons();
        BuildGroupCards();
    }

    private void ShowRelicStep(AncientRelicGroupOption group)
    {
        _currentGroup = group;
        _groupContent.Visible = false;
        _relicContent.Visible = true;
        _backButton.Visible = true;
        _titleLabel.Text = _relicTitle;
        _subtitleLabel.SetTextAutoSize(string.Format(_relicSubtitle, group.Title));
        ClearButtons();
        BuildRelicCards(group);
    }

    private void BuildGroupCards()
    {
        foreach (var child in _groupContent.GetChildren())
            child.QueueFree();

        var cards = new List<Control>(_groupOptions.Count);
        for (var index = 0; index < _groupOptions.Count; index++)
        {
            var group = _groupOptions[index];
            var button = CreateGroupCard(group, index);
            _groupContent.AddChild(button);
            cards.Add(button);
        }

        ApplyCenteredCardLayout(cards, _groupOptions.Count, GroupCardWidth, GroupCardHeight, 4, 286f, 346f, 36f);
        ConfigureGridFocus(cards.OfType<Button>().ToList(), 4);
    }

    private void BuildRelicCards(AncientRelicGroupOption group)
    {
        foreach (var child in _relicContent.GetChildren())
            child.QueueFree();

        var cards = new List<Control>(group.Relics.Count);
        for (var index = 0; index < group.Relics.Count; index++)
        {
            var relic = group.Relics[index];
            var button = CreateRelicCard(relic, index);
            _relicContent.AddChild(button);
            cards.Add(button);
        }

        ApplyCenteredCardLayout(cards, group.Relics.Count, HolderWidth, HolderHeight, 8, 176f, 190f, -30f);
        ConfigureGridFocus(cards.OfType<Button>().ToList(), Math.Min(8, Math.Max(1, group.Relics.Count)));
    }

    private Button CreateGroupCard(AncientRelicGroupOption group, int index)
    {
        var button = new Button
        {
            Name = $"GroupedRelicGroup_{index}",
            CustomMinimumSize = new Vector2(GroupCardWidth, GroupCardHeight),
            Flat = true,
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop
        };
        ApplyTransparentButton(button);

        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(GroupCardWidth, GroupCardHeight)
        };
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", CreateGroupPanelStyle(false));
        button.AddChild(panel);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.OffsetLeft = 16f;
        root.OffsetTop = 14f;
        root.OffsetRight = -16f;
        root.OffsetBottom = -14f;
        root.AddThemeConstantOverride("separation", 12);
        panel.AddChild(root);

        var title = new Label
        {
            Text = group.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        ApplyTitleTheme(title);
        root.AddChild(title);

        var iconCenter = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(GroupIconSize, GroupIconSize)
        };
        var baseTexture = CreateRelicBaseTexture(group.RepresentativeRelic, GroupIconSize + 18f);
        if (baseTexture != null)
            iconCenter.AddChild(baseTexture);
        var icon = CreateRelicTexture(group.RepresentativeRelic, GroupIconSize);
        iconCenter.AddChild(icon);
        root.AddChild(iconCenter);

        var count = new Label
        {
            Text = string.Format(ProphecySoulDevourRegistry.AncientRelicGroupCountText.GetFormattedText(), group.Relics.Count),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        count.AddThemeFontSizeOverride("font_size", 20);
        count.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.92f));
        root.AddChild(count);

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
            MaxFontSize = 18,
            MinFontSize = 14,
            CustomMinimumSize = new Vector2(180f, 66f)
        };
        ApplyDefaultMegaRichTextTheme(description);
        description.AddThemeColorOverride("default_color", new Color(0.95f, 0.95f, 0.95f));
        description.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0f));
        description.AddThemeConstantOverride("shadow_offset_x", 0);
        description.AddThemeConstantOverride("shadow_offset_y", 0);
        description.SetTextAutoSize(group.Description);
        descriptionPanel.AddChild(description);

        button.Pressed += () => ShowRelicStep(group);
        button.MouseEntered += () => SetGroupCardHoverState(panel, icon, descriptionPanel, true);
        button.MouseExited += () => SetGroupCardHoverState(panel, icon, descriptionPanel, false);
        _buttonsByIndex[index] = button;
        _texturesByIndex[index] = icon;
        return button;
    }

    private Button CreateRelicCard(RelicModel relic, int index)
    {
        var button = new Button
        {
            Name = $"GroupedRelic_{index}",
            CustomMinimumSize = new Vector2(HolderWidth, HolderHeight),
            Flat = true,
            FocusMode = FocusModeEnum.All,
            MouseFilter = MouseFilterEnum.Stop
        };
        ApplyTransparentButton(button);

        var baseTexture = CreateRelicBaseTexture(relic, HolderWidth);
        if (baseTexture != null)
        {
            baseTexture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            button.AddChild(baseTexture);
        }

        var icon = CreateRelicTexture(relic, RelicIconSize);
        icon.SetAnchorsAndOffsetsPreset(LayoutPreset.Center);
        icon.OffsetLeft = -RelicIconSize * 0.5f;
        icon.OffsetTop = -RelicIconSize * 0.5f;
        icon.OffsetRight = RelicIconSize * 0.5f;
        icon.OffsetBottom = RelicIconSize * 0.5f;
        button.AddChild(icon);

        button.Pressed += () => Complete(relic);
        button.MouseEntered += () => ShowHoverTip(button, icon, relic);
        button.MouseExited += () => HideHoverTip(button, icon);
        _buttonsByIndex[index] = button;
        _texturesByIndex[index] = icon;
        return button;
    }

    private void OnBackPressed()
    {
        if (_choiceLocked)
            return;

        ShowGroupStep();
    }

    private void OnSkipPressed()
    {
        if (_choiceLocked || !_allowSkip)
            return;

        Complete(null);
    }

    private void Complete(RelicModel? selectedRelic)
    {
        if (_choiceLocked)
            return;

        _choiceLocked = true;
        _completionSource.TrySetResult(selectedRelic);
        Close();
    }

    private void EnsureCompletedOnForcedClose()
    {
        if (_completionSource.Task.IsCompleted)
            return;

        _completionSource.TrySetResult(null);
    }

    private void ClearButtons()
    {
        _buttonsByIndex.Clear();
        _texturesByIndex.Clear();
    }

    private static void ApplyCenteredCardLayout(
        IReadOnlyList<Control> cards,
        int optionCount,
        float width,
        float height,
        int maxColumns,
        float horizontalSpacing,
        float verticalSpacing,
        float yOffset)
    {
        var columns = Math.Min(maxColumns, Math.Max(1, optionCount));
        var rows = (int)Math.Ceiling(optionCount / (double)columns);

        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var row = index / columns;
            var column = index % columns;
            var rowCount = row == rows - 1 ? optionCount - row * columns : columns;
            var x = (column - (rowCount - 1) * 0.5f) * horizontalSpacing - width * 0.5f;
            var y = (row - (rows - 1) * 0.5f) * verticalSpacing - height * 0.5f + yOffset;

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

    private void ConfigureGridFocus(IReadOnlyList<Button> buttons, int columns)
    {
        if (buttons.Count == 0)
            return;

        for (var i = 0; i < buttons.Count; i++)
        {
            var left = i > 0 ? i - 1 : buttons.Count - 1;
            var right = i < buttons.Count - 1 ? i + 1 : 0;
            var up = i - columns >= 0 ? i - columns : i;
            var down = i + columns < buttons.Count ? i + columns : i;

            buttons[i].FocusNeighborLeft = buttons[left].GetPath();
            buttons[i].FocusNeighborRight = buttons[right].GetPath();
            buttons[i].FocusNeighborTop = buttons[up].GetPath();
            buttons[i].FocusNeighborBottom = buttons[down].GetPath();
        }

        if (_backButton.Visible)
        {
            buttons[0].FocusNeighborBottom = _backButton.GetPath();
            _backButton.FocusNeighborTop = buttons[0].GetPath();
            _backButton.FocusNeighborRight = _skipButton.GetPath();
        }

        if (_skipButton.Visible)
        {
            buttons[^1].FocusNeighborBottom = _skipButton.GetPath();
            _skipButton.FocusNeighborTop = buttons[^1].GetPath();
            _skipButton.FocusNeighborLeft = _backButton.Visible ? _backButton.GetPath() : buttons[0].GetPath();
        }
    }

    private static void ShowHoverTip(Control owner, TextureRect relicTexture, RelicModel relic)
    {
        relicTexture.Scale = Vector2.One * 1.08f;
        var tipSet = NHoverTipSet.CreateAndShow(owner, relic.HoverTips, HoverTip.GetHoverTipAlignment(owner));
        tipSet?.SetAlignment(relicTexture, HoverTip.GetHoverTipAlignment(owner));
    }

    private static void HideHoverTip(Control owner, TextureRect relicTexture)
    {
        relicTexture.Scale = Vector2.One;
        NHoverTipSet.Remove(owner);
    }

    private static void SetGroupCardHoverState(PanelContainer panel, TextureRect icon, PanelContainer descriptionPanel, bool active)
    {
        panel.AddThemeStyleboxOverride("panel", CreateGroupPanelStyle(active));
        descriptionPanel.AddThemeStyleboxOverride("panel", CreateDescriptionPanelStyle(active));
        icon.Scale = active ? Vector2.One * 1.06f : Vector2.One;
    }

    private static Button CreateBottomButton(string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(200f, 58f)
        };
        button.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        button.OffsetTop = -92f;
        button.OffsetBottom = -28f;
        button.AddThemeFontSizeOverride("font_size", 24);
        button.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_hover_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_pressed_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeColorOverride("font_focus_color", new Color(0.08f, 0.08f, 0.08f));
        button.AddThemeStyleboxOverride("normal", CreateBottomButtonStyle(new Color(0.96f, 0.82f, 0.2f), new Color(1f, 0.91f, 0.5f)));
        button.AddThemeStyleboxOverride("hover", CreateBottomButtonStyle(new Color(1f, 0.86f, 0.26f), new Color(1f, 0.95f, 0.58f)));
        button.AddThemeStyleboxOverride("pressed", CreateBottomButtonStyle(new Color(0.92f, 0.76f, 0.14f), new Color(1f, 0.88f, 0.45f)));
        button.AddThemeStyleboxOverride("focus", CreateBottomButtonStyle(new Color(1f, 0.86f, 0.26f), new Color(1f, 0.95f, 0.58f)));
        return button;
    }

    private static void ApplyTransparentButton(Button button)
    {
        var empty = new StyleBoxEmpty();
        button.AddThemeStyleboxOverride("normal", empty);
        button.AddThemeStyleboxOverride("hover", empty);
        button.AddThemeStyleboxOverride("pressed", empty);
        button.AddThemeStyleboxOverride("focus", empty);
    }

    private static void ApplyTitleTheme(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 24);
        label.AddThemeColorOverride("font_color", new Color(1f, 0.88f, 0.28f));
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

    private static TextureRect? CreateRelicBaseTexture(RelicModel relic, float sideLength)
    {
        var path = relic.Rarity switch
        {
            RelicRarity.Common => CommonRelicBasePath,
            RelicRarity.Uncommon => UncommonRelicBasePath,
            RelicRarity.Rare or RelicRarity.Ancient => RareRelicBasePath,
            _ => null
        };
        if (path == null)
            return null;

        return new TextureRect
        {
            Texture = GD.Load<Texture2D>(path),
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(sideLength, sideLength),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
        };
    }

    private static TextureRect CreateRelicTexture(RelicModel relic, float sideLength)
    {
        return new TextureRect
        {
            Texture = relic.BigIcon ?? relic.Icon,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(sideLength, sideLength),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
        };
    }

    private static StyleBoxFlat CreateGroupPanelStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? new Color(0.12f, 0.16f, 0.22f, 0.96f) : new Color(0.09f, 0.12f, 0.16f, 0.94f),
            BorderColor = active ? new Color(0.56f, 0.76f, 1f, 0.96f) : new Color(0.34f, 0.46f, 0.62f, 0.88f),
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

    private static StyleBoxFlat CreateDescriptionPanelStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? new Color(0f, 0f, 0f, 0.26f) : new Color(0f, 0f, 0f, 0.18f)
        };
        style.SetCornerRadiusAll(14);
        style.ContentMarginLeft = 14f;
        style.ContentMarginRight = 14f;
        style.ContentMarginTop = 10f;
        style.ContentMarginBottom = 10f;
        return style;
    }

    private static StyleBoxFlat CreateBottomButtonStyle(Color background, Color border)
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
        style.ContentMarginLeft = 8f;
        style.ContentMarginRight = 8f;
        style.ContentMarginTop = 4f;
        style.ContentMarginBottom = 4f;
        return style;
    }
}
