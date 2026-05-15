using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public sealed partial class RefreshableTokenRelicSelectionScreen : Control, IOverlayScreen, IScreenContext
{
    private readonly TaskCompletionSource<RefreshableTokenRelicSelectionResult> _completionSource = new();
    private readonly RunState _runState;
    private readonly Player _owner;
    private readonly Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> _rerollFunc;
    private readonly List<int> _rerollHistory = [];
    private readonly Dictionary<int, Button> _holdersByIndex = [];
    private readonly Dictionary<int, TextureRect> _relicTexturesByIndex = [];
    private readonly HashSet<ModelId> _seenOptionIds = [];
    private IReadOnlyList<RelicModel> _options;
    private readonly int _startingRerolls;
    private readonly string _subtitlePrefix;
    private readonly string _probabilityText;
    private int _remainingRerolls;

    private VBoxContainer _rootContainer = null!;
    private HBoxContainer _holderContainer = null!;
    private Label _titleLabel = null!;
    private Label _infoLabel = null!;
    private Label _probabilityLabel = null!;
    private Label _bottomPromptLabel = null!;
    private Button _rerollButton = null!;
    private bool _closed;
    private bool _choiceLocked;
    private bool _holdersBuilt;

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _holdersByIndex.Count > 0 ? _holdersByIndex[0] : _rerollButton;

    private RefreshableTokenRelicSelectionScreen(
        Player owner,
        IReadOnlyList<RelicModel> options,
        int rerollCount,
        string title,
        string subtitlePrefix,
        string probabilityText,
        Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> rerollFunc)
    {
        _owner = owner;
        _runState = (RunState)owner.RunState;
        _options = options;
        _startingRerolls = Math.Max(0, rerollCount);
        _remainingRerolls = _startingRerolls;
        _subtitlePrefix = subtitlePrefix;
        _probabilityText = probabilityText;
        _rerollFunc = rerollFunc;

        Name = nameof(RefreshableTokenRelicSelectionScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;

        BuildUi(title);
        UpdateSubtitle();
    }

    public static RefreshableTokenRelicSelectionScreen Create(
        Player owner,
        IReadOnlyList<RelicModel> options,
        int rerollCount,
        string title,
        string subtitlePrefix,
        string probabilityText,
        Func<IReadOnlyList<RelicModel>, int, IReadOnlySet<ModelId>, IReadOnlyList<RelicModel>> rerollFunc)
    {
        return new RefreshableTokenRelicSelectionScreen(owner, options, rerollCount, title, subtitlePrefix,
            probabilityText, rerollFunc);
    }

    public Task<RefreshableTokenRelicSelectionResult> WaitForResult()
    {
        return _completionSource.Task;
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
        BuildRelicHoldersIfNeeded();
    }

    public void AfterOverlayClosed()
    {
        _closed = true;
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

    private void BuildUi(string title)
    {
        var backstop = new ColorRect
        {
            Color = new Color(0.03f, 0.03f, 0.05f, 0.62f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        backstop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(backstop);

        _titleLabel = new Label
        {
            Name = "Title",
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderLarge",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _titleLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterTop);
        _titleLabel.OffsetTop = 18f;
        _titleLabel.OffsetLeft = -420f;
        _titleLabel.OffsetRight = 420f;
        _titleLabel.OffsetBottom = 84f;
        AddChild(_titleLabel);

        _infoLabel = new Label
        {
            Name = "Info",
            Text = $"{_subtitlePrefix}{_remainingRerolls}",
            HorizontalAlignment = HorizontalAlignment.Right,
            ThemeTypeVariation = "HeaderSmall",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _infoLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _infoLabel.OffsetTop = 28f;
        _infoLabel.OffsetLeft = -540f;
        _infoLabel.OffsetRight = -40f;
        AddChild(_infoLabel);

        _probabilityLabel = new Label
        {
            Name = "Probability",
            Text = _probabilityText,
            HorizontalAlignment = HorizontalAlignment.Right,
            ThemeTypeVariation = "HeaderSmall",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _probabilityLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _probabilityLabel.OffsetTop = 56f;
        _probabilityLabel.OffsetLeft = -540f;
        _probabilityLabel.OffsetRight = -40f;
        AddChild(_probabilityLabel);

        _rootContainer = new VBoxContainer
        {
            Name = "RootContainer",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _rootContainer.AddThemeConstantOverride("separation", 10);
        _rootContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _rootContainer.OffsetTop = 76f;
        _rootContainer.OffsetBottom = -104f;
        AddChild(_rootContainer);

        var selectionRow = new HBoxContainer
        {
            Name = "SelectionRow",
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        selectionRow.AddThemeConstantOverride("separation", 26);

        _holderContainer = new HBoxContainer
        {
            Name = "HolderContainer",
            MouseFilter = MouseFilterEnum.Pass,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        _holderContainer.AddThemeConstantOverride("separation", 132);
        _rootContainer.AddChild(new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0f, 18f)
        });
        _rootContainer.AddChild(selectionRow);
        _rootContainer.AddChild(new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(0f, 10f)
        });

        selectionRow.AddChild(new Control
        {
            Name = "LeftBalanceSpacer",
            CustomMinimumSize = new Vector2(146f, 0f),
            MouseFilter = MouseFilterEnum.Ignore
        });
        selectionRow.AddChild(_holderContainer);

        _rerollButton = new Button
        {
            Text = "刷新",
            CustomMinimumSize = new Vector2(134f, 126f)
        };
        _rerollButton.Pressed += OnRerollPressed;
        _rerollButton.AddThemeStyleboxOverride("normal",
            CreateRerollButtonStyle(new Color(0.98f, 0.82f, 0.16f), new Color(1f, 0.9f, 0.4f)));
        _rerollButton.AddThemeStyleboxOverride("hover",
            CreateRerollButtonStyle(new Color(1f, 0.86f, 0.22f), new Color(1f, 0.94f, 0.5f)));
        _rerollButton.AddThemeStyleboxOverride("pressed",
            CreateRerollButtonStyle(new Color(0.92f, 0.76f, 0.1f), new Color(1f, 0.88f, 0.36f)));
        _rerollButton.AddThemeStyleboxOverride("focus",
            CreateRerollButtonStyle(new Color(1f, 0.86f, 0.22f), new Color(1f, 0.94f, 0.5f)));
        _rerollButton.AddThemeStyleboxOverride("disabled",
            CreateRerollButtonStyle(new Color(0.56f, 0.5f, 0.34f, 0.86f), new Color(0.72f, 0.66f, 0.48f, 0.92f)));
        _rerollButton.AddThemeFontSizeOverride("font_size", 34);
        _rerollButton.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
        _rerollButton.AddThemeColorOverride("font_hover_color", new Color(0.08f, 0.08f, 0.08f));
        _rerollButton.AddThemeColorOverride("font_pressed_color", new Color(0.08f, 0.08f, 0.08f));
        _rerollButton.AddThemeColorOverride("font_focus_color", new Color(0.08f, 0.08f, 0.08f));
        _rerollButton.AddThemeColorOverride("font_disabled_color", new Color(0.16f, 0.16f, 0.16f, 0.72f));
        var rerollColumn = new VBoxContainer
        {
            Name = "RerollColumn",
            MouseFilter = MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(146f, 0f),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        rerollColumn.AddThemeConstantOverride("separation", 0);
        var rerollCenter = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsVertical = SizeFlags.ShrinkCenter
        };
        rerollCenter.AddChild(_rerollButton);
        rerollColumn.AddChild(new Control
            { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        rerollColumn.AddChild(rerollCenter);
        rerollColumn.AddChild(new Control
            { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });
        selectionRow.AddChild(rerollColumn);

        var bottomPromptPanel = new PanelContainer
        {
            Name = "BottomPrompt",
            MouseFilter = MouseFilterEnum.Ignore
        };
        bottomPromptPanel.SetAnchorsAndOffsetsPreset(LayoutPreset.CenterBottom);
        bottomPromptPanel.OffsetLeft = -160f;
        bottomPromptPanel.OffsetRight = 160f;
        bottomPromptPanel.OffsetTop = -92f;
        bottomPromptPanel.OffsetBottom = -24f;
        AddChild(bottomPromptPanel);

        _bottomPromptLabel = new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ThemeTypeVariation = "HeaderMedium",
            MouseFilter = MouseFilterEnum.Ignore
        };
        _bottomPromptLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bottomPromptPanel.AddChild(_bottomPromptLabel);
    }

    private void BuildRelicHoldersIfNeeded()
    {
        if (_holdersBuilt)
            return;

        _holdersBuilt = true;
        BuildRelicHolders();
    }

    private void BuildRelicHolders()
    {
        ClearRelicHolders();
        for (var index = 0; index < _options.Count; index++)
        {
            var relic = _options[index];
            var capturedIndex = index;

            var optionColumn = new VBoxContainer
            {
                Name = $"RefreshableTokenOption_{index}",
                CustomMinimumSize = new Vector2(340f, 0f),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                MouseFilter = MouseFilterEnum.Pass
            };
            optionColumn.AddThemeConstantOverride("separation", 10);
            _holderContainer.AddChild(optionColumn);

            var title = new Label
            {
                Text = relic.Title.GetFormattedText(),
                HorizontalAlignment = HorizontalAlignment.Center,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                Modulate = new Color(1f, 0.86f, 0.2f),
                CustomMinimumSize = new Vector2(340f, 48f),
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            ApplyUniformTitleTheme(title);
            optionColumn.AddChild(title);

            var buttonCenter = new CenterContainer
            {
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0f, 196f)
            };
            optionColumn.AddChild(buttonCenter);

            var holderButton = new Button
            {
                Name = $"RefreshableTokenRelicHolder_{index}",
                CustomMinimumSize = new Vector2(230f, 230f),
                Flat = true,
                MouseFilter = MouseFilterEnum.Stop,
                FocusMode = FocusModeEnum.All
            };
            var empty = new StyleBoxEmpty();
            holderButton.AddThemeStyleboxOverride("normal", empty);
            holderButton.AddThemeStyleboxOverride("hover", empty);
            holderButton.AddThemeStyleboxOverride("pressed", empty);
            holderButton.AddThemeStyleboxOverride("focus", empty);
            buttonCenter.AddChild(holderButton);

            var iconCenter = new CenterContainer
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            iconCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            holderButton.AddChild(iconCenter);

            var relicTexture = CreateRelicTexture(relic, 176f);
            iconCenter.AddChild(relicTexture);

            var description = new MegaRichTextLabel
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(320f, 118f),
                MaxFontSize = 21,
                MinFontSize = 15,
                MouseFilter = MouseFilterEnum.Ignore,
                BbcodeEnabled = true
            };
            ApplyDefaultMegaRichTextTheme(description);
            description.AddThemeColorOverride("default_color", new Color(0.9f, 0.93f, 0.97f, 0.92f));
            description.SetTextAutoSize(relic.DynamicDescription.GetFormattedText());
            optionColumn.AddChild(description);

            holderButton.Pressed += () => OnRelicSelected(capturedIndex);
            holderButton.MouseEntered += () => ShowHoverTip(holderButton, relicTexture, relic);
            holderButton.MouseExited += () => HideHoverTip(holderButton, relicTexture);

            _holdersByIndex[index] = holderButton;
            _relicTexturesByIndex[index] = relicTexture;
            _seenOptionIds.Add(relic.CanonicalInstance?.Id ?? relic.Id);
        }

        ConfigureHolderFocusNeighbors();
    }

    private void ClearRelicHolders()
    {
        foreach (var child in _holderContainer.GetChildren())
            child.QueueFree();

        _holdersByIndex.Clear();
        _relicTexturesByIndex.Clear();
    }

    private void OnRerollPressed()
    {
        if (_choiceLocked || _remainingRerolls <= 0)
            return;

        var rerollOrdinal = _rerollHistory.Count;
        var rerolled = _rerollFunc(_options, rerollOrdinal, _seenOptionIds);
        if (rerolled.Count == 0)
            return;

        _options = rerolled;
        _rerollHistory.Add(rerollOrdinal);
        _remainingRerolls = Math.Max(0, _remainingRerolls - 1);
        foreach (var relic in _options)
            _seenOptionIds.Add(relic.CanonicalInstance?.Id ?? relic.Id);
        BuildRelicHolders();
        UpdateSubtitle();
    }

    private void OnRelicSelected(int index)
    {
        if (_choiceLocked || index < 0 || index >= _options.Count)
            return;

        _choiceLocked = true;
        Complete(_options[index]);
    }

    private void Complete(RelicModel? selectedRelic)
    {
        var selectedIndex = selectedRelic == null ? -1 : IndexOfRelic(_options, selectedRelic);
        _completionSource.TrySetResult(new RefreshableTokenRelicSelectionResult
        {
            SelectedRelic = selectedRelic,
            SelectedIndex = selectedIndex,
            StartingRerolls = _startingRerolls,
            RemainingRerolls = _remainingRerolls,
            RerollHistory = _rerollHistory.ToList(),
            FinalOptions = _options.ToList()
        });
        Close();
    }

    private void UpdateSubtitle()
    {
        _rerollButton.Text = $"刷新\nx{_remainingRerolls}";
        _infoLabel.Text = $"{_subtitlePrefix}{_remainingRerolls}";
        _rerollButton.Disabled = _choiceLocked || _remainingRerolls <= 0;
    }

    private void ConfigureHolderFocusNeighbors()
    {
        var holders = _holdersByIndex.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        if (holders.Count == 0)
            return;

        for (var i = 0; i < holders.Count; i++)
        {
            holders[i].FocusNeighborTop = holders[i].GetPath();
            holders[i].FocusNeighborBottom = _rerollButton.GetPath();
            holders[i].FocusNeighborLeft = holders[(i - 1 + holders.Count) % holders.Count].GetPath();
            holders[i].FocusNeighborRight = holders[(i + 1) % holders.Count].GetPath();
        }

        _rerollButton.FocusNeighborTop = holders[0].GetPath();
        _rerollButton.FocusNeighborLeft = holders[^1].GetPath();
        _rerollButton.FocusNeighborRight = holders[0].GetPath();
    }

    private static int IndexOfRelic(IReadOnlyList<RelicModel> relics, RelicModel relic)
    {
        for (var i = 0; i < relics.Count; i++)
        {
            var left = relics[i].CanonicalInstance?.Id ?? relics[i].Id;
            var right = relic.CanonicalInstance?.Id ?? relic.Id;
            if (left == right)
                return i;
        }

        return -1;
    }

    private static void ShowHoverTip(Control owner, TextureRect relicTexture, RelicModel relic)
    {
        relicTexture.Scale = Vector2.One * 1.06f;
        var tipSet = NHoverTipSet.CreateAndShow(owner, relic.HoverTips, HoverTip.GetHoverTipAlignment(owner));
        tipSet?.SetAlignment(relicTexture, HoverTip.GetHoverTipAlignment(owner));
    }

    private static void HideHoverTip(Control owner, TextureRect relicTexture)
    {
        relicTexture.Scale = Vector2.One;
        NHoverTipSet.Remove(owner);
    }

    private static void ApplyDefaultMegaLabelTheme(MegaLabel label)
    {
        var font = label.GetThemeDefaultFont();
        if (font != null)
            label.AddThemeFontOverride("font", font);

        var fontSize = label.GetThemeDefaultFontSize();
        if (fontSize > 0)
            label.AddThemeFontSizeOverride("font_size", fontSize);
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

    private static void ApplyUniformTitleTheme(Label label)
    {
        var font = label.GetThemeDefaultFont();
        if (font != null)
            label.AddThemeFontOverride("font", font);

        label.AddThemeFontSizeOverride("font_size", 34);
    }

    private static Texture2D? GetDisplayTexture(RelicModel relic)
    {
        return relic.BigIcon ?? relic.Icon;
    }

    private static TextureRect CreateRelicTexture(RelicModel relic, float sideLength)
    {
        return new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = GetDisplayTexture(relic),
            CustomMinimumSize = new Vector2(sideLength, sideLength),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
        };
    }

    private static StyleBoxFlat CreateRerollButtonStyle(Color background, Color border)
    {
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            ShadowColor = new Color(0f, 0f, 0f, 0.16f),
            ShadowSize = 8,
            ShadowOffset = new Vector2(0f, 4f)
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(18);
        style.ContentMarginLeft = 6;
        style.ContentMarginRight = 6;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        return style;
    }
}

public sealed class RefreshableTokenRelicSelectionResult
{
    public required RelicModel? SelectedRelic { get; init; }
    public required int SelectedIndex { get; init; }
    public required int StartingRerolls { get; init; }
    public required int RemainingRerolls { get; init; }
    public required IReadOnlyList<int> RerollHistory { get; init; }
    public required IReadOnlyList<RelicModel> FinalOptions { get; init; }
}
