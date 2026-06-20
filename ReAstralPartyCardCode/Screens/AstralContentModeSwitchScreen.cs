using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Screens;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Screens;

public sealed partial class AstralContentModeSwitchScreen : Control, ICapstoneScreen, IScreenContext
{
    private const string SettingsLocTable = "settings_ui";
    private const string LocPrefix = "RE_ASTRAL_PARTY_MOD_SETTINGS.content_mode_screen";
    private static readonly Color BackgroundColor = new(0.03f, 0.04f, 0.05f, 0.94f);
    private static readonly Color PanelColor = new(0.11f, 0.13f, 0.15f, 0.98f);
    private static readonly Color PanelBorderColor = new(0.34f, 0.44f, 0.58f, 1f);
    private static readonly Color ActiveCardBorderColor = new(0.92f, 0.72f, 0.28f, 1f);
    private static readonly Color ActiveCardFillColor = new(0.19f, 0.16f, 0.08f, 0.95f);
    private static readonly Color InactiveCardFillColor = new(0.16f, 0.18f, 0.21f, 0.96f);
    private static readonly Color InactiveCardBorderColor = new(0.42f, 0.48f, 0.58f, 0.8f);
    private static AstralContentModeSwitchScreen? _instance;

    private Action _closeAction = static () => { };
    private Button _closeButton = null!;
    private readonly List<ModeCardView> _modeCards = [];

    public NetScreenType ScreenType => NetScreenType.None;
    public bool UseSharedBackstop => true;
    public Control? DefaultFocusedControl => _closeButton;

    private sealed class ModeCardView
    {
        public required AstralContentMode Mode { get; init; }
        public required Button Button { get; init; }
        public required Label StateLabel { get; init; }
    }

    private AstralContentModeSwitchScreen()
    {
        Name = nameof(AstralContentModeSwitchScreen);
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        FocusMode = FocusModeEnum.All;
        Visible = false;
        BuildUi();
    }

    public static AstralContentModeSwitchScreen GetOrCreate()
    {
        if (_instance == null || !GodotObject.IsInstanceValid(_instance))
            _instance = new AstralContentModeSwitchScreen();

        _instance.SetCloseAction(static () => ModScreenService.Close());
        _instance.RefreshCards();
        return _instance;
    }

    public void ShowStandalone()
    {
        Visible = true;
        RefreshCards();
        _closeButton.GrabFocus();
    }

    public void HideStandalone()
    {
        Visible = false;
    }

    private void SetCloseAction(Action closeAction)
    {
        _closeAction = closeAction ?? throw new ArgumentNullException(nameof(closeAction));
    }

    private void BuildUi()
    {
        var backdrop = new ColorRect
        {
            Color = BackgroundColor,
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

        var frame = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Stop,
            CustomMinimumSize = new Vector2(1120f, 720f)
        };
        frame.AddThemeStyleboxOverride("panel", CreateFrameStyle());
        center.AddChild(frame);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        root.AddThemeConstantOverride("separation", 20);
        frame.AddChild(root);

        var header = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        header.AddThemeConstantOverride("separation", 16);
        root.AddChild(header);

        var titleColumn = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleColumn.AddThemeConstantOverride("separation", 6);
        header.AddChild(titleColumn);

        titleColumn.AddChild(CreateHeaderLabel($"{LocPrefix}.title", 34, new Color(0.95f, 0.95f, 0.93f, 1f)));
        titleColumn.AddChild(CreateHeaderLabel($"{LocPrefix}.subtitle", 18, new Color(0.78f, 0.8f, 0.83f, 1f)));

        _closeButton = new Button
        {
            Text = Loc($"{LocPrefix}.close"),
            CustomMinimumSize = new Vector2(120f, 52f),
            FocusMode = FocusModeEnum.All
        };
        _closeButton.Pressed += () => _closeAction();
        _closeButton.AddThemeStyleboxOverride("normal", CreateButtonStyle(new Color(0.25f, 0.26f, 0.29f, 1f)));
        _closeButton.AddThemeStyleboxOverride("hover", CreateButtonStyle(new Color(0.32f, 0.33f, 0.37f, 1f)));
        _closeButton.AddThemeStyleboxOverride("pressed", CreateButtonStyle(new Color(0.21f, 0.22f, 0.25f, 1f)));
        _closeButton.AddThemeStyleboxOverride("focus", CreateButtonStyle(new Color(0.32f, 0.33f, 0.37f, 1f)));
        header.AddChild(_closeButton);

        var body = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 18);
        root.AddChild(body);

        var intro = new Label
        {
            Text = Loc($"{LocPrefix}.intro"),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore
        };
        intro.AddThemeFontSizeOverride("font_size", 18);
        intro.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.88f, 0.95f));
        body.AddChild(intro);

        var cards = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        cards.AddThemeConstantOverride("separation", 24);
        body.AddChild(cards);

        foreach (var definition in AstralContentModeRegistry.Modes)
        {
            var card = BuildModeCard(definition.Mode, definition.TitleLocKey, definition.DescriptionLocKey);
            _modeCards.Add(card);
            cards.AddChild(card.Button);
        }
    }

    private ModeCardView BuildModeCard(
        AstralContentMode mode,
        string titleKey,
        string descriptionKey)
    {
        var button = new Button
        {
            Flat = true,
            FocusMode = FocusModeEnum.All,
            CustomMinimumSize = new Vector2(420f, 360f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        button.Pressed += () => OnModeSelected(mode);

        var shell = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        shell.AddThemeConstantOverride("separation", 14);
        button.AddChild(shell);

        var title = new Label
        {
            Text = Loc(titleKey),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        title.AddThemeColorOverride("font_color", Colors.White);
        shell.AddChild(title);

        var description = new Label
        {
            Text = Loc(descriptionKey),
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        description.AddThemeFontSizeOverride("font_size", 18);
        description.AddThemeColorOverride("font_color", new Color(0.86f, 0.88f, 0.91f, 0.96f));
        shell.AddChild(description);

        var state = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        state.AddThemeFontSizeOverride("font_size", 18);
        shell.AddChild(state);

        return new ModeCardView
        {
            Mode = mode,
            Button = button,
            StateLabel = state
        };
    }

    private void OnModeSelected(AstralContentMode mode)
    {
        ReAstralPartyModSettingsManager.SetCurrentContentMode(mode);
        LobbyGameplaySettingsSync.ResetForContentModeSwitch();
        CharacterSelectGameplayPreviewPanelRuntimeBridge.RequestRefreshAll();
        RefreshCards();
    }

    private void RefreshCards()
    {
        var currentMode = ReAstralPartyModSettingsManager.GetCurrentContentMode();
        foreach (var card in _modeCards)
        {
            var active = card.Mode == currentMode;
            card.StateLabel.Text = active
                ? Loc($"{LocPrefix}.current_mode")
                : Loc($"{LocPrefix}.switch_to_mode");
            card.StateLabel.AddThemeColorOverride("font_color", active
                ? new Color(0.95f, 0.84f, 0.42f, 1f)
                : new Color(0.74f, 0.82f, 0.92f, 1f));
            card.Button.AddThemeStyleboxOverride("normal", CreateModeCardStyle(active));
            card.Button.AddThemeStyleboxOverride("hover", CreateModeCardStyle(true));
            card.Button.AddThemeStyleboxOverride("pressed", CreateModeCardStyle(true));
            card.Button.AddThemeStyleboxOverride("focus", CreateModeCardStyle(true));
        }
    }

    private static StyleBoxFlat CreateFrameStyle()
    {
        var style = new StyleBoxFlat
        {
            BgColor = PanelColor,
            BorderColor = PanelBorderColor,
            ContentMarginLeft = 28f,
            ContentMarginTop = 24f,
            ContentMarginRight = 28f,
            ContentMarginBottom = 24f
        };
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(8);
        return style;
    }

    private static StyleBoxFlat CreateButtonStyle(Color backgroundColor)
    {
        var style = new StyleBoxFlat
        {
            BgColor = backgroundColor,
            BorderColor = new Color(0.54f, 0.55f, 0.58f, 1f),
            ContentMarginLeft = 14f,
            ContentMarginTop = 8f,
            ContentMarginRight = 14f,
            ContentMarginBottom = 8f
        };
        style.SetBorderWidthAll(2);
        style.SetCornerRadiusAll(6);
        return style;
    }

    private static StyleBoxFlat CreateModeCardStyle(bool active)
    {
        var style = new StyleBoxFlat
        {
            BgColor = active ? ActiveCardFillColor : InactiveCardFillColor,
            BorderColor = active ? ActiveCardBorderColor : InactiveCardBorderColor,
            ContentMarginLeft = 20f,
            ContentMarginTop = 18f,
            ContentMarginRight = 20f,
            ContentMarginBottom = 18f
        };
        style.SetBorderWidthAll(3);
        style.SetCornerRadiusAll(10);
        return style;
    }

    private static Label CreateHeaderLabel(string locKey, int fontSize, Color fontColor)
    {
        var label = new Label
        {
            Text = Loc(locKey),
            MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", fontColor);
        return label;
    }

    private static string Loc(string key)
    {
        return new LocString(SettingsLocTable, key).GetRawText();
    }

    public void AfterCapstoneOpened()
    {
        ShowStandalone();
    }

    public void AfterCapstoneClosed()
    {
        HideStandalone();
        if (ReferenceEquals(_instance, this))
            _instance = null;
        QueueFree();
    }
}
