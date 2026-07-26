using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

internal static partial class FinalArenaDistortionComponentPopup
{
    private const string IconPath = "res://ReAstralPartyMod/images/ui/distortion_component_logo.png";
    private const string CloseButtonPath = "res://ReAstralPartyMod/images/distortion/distortion_close.png";
    private const string HoverTipRoot = "RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT";

    public static void Open(IRunState runState)
    {
        if (!FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.HumanAiDuel) &&
            !FinalArenaSeriesHelper.TryGetActiveComponents(runState, out _))
            return;
        FinalArenaSeriesHelper.TryGetActiveComponents(runState, out var activeComponents);
        if (NModalContainer.Instance == null)
            return;

        NModalContainer.Instance.Clear();
        NModalContainer.Instance.Add(new FinalArenaDistortionComponentPopupPanel(
            FinalArenaSeriesHelper.GetAllComponentsInDisplayOrder(),
            activeComponents));
        NModalContainer.Instance.ShowBackstop();
    }

    private sealed partial class FinalArenaDistortionComponentPopupPanel : Control, IScreenContext
    {
        private readonly IReadOnlyList<FinalArenaDistortionComponent> _components;
        private readonly IReadOnlySet<FinalArenaDistortionComponent> _activeComponents;

        public FinalArenaDistortionComponentPopupPanel(
            IReadOnlyList<FinalArenaDistortionComponent> components,
            IReadOnlyList<FinalArenaDistortionComponent> activeComponents)
        {
            _components = components;
            _activeComponents = activeComponents.ToHashSet();
        }

        public Control? DefaultFocusedControl { get; private set; }

        public override void _Ready()
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Stop;
            FocusMode = FocusModeEnum.All;
            SetProcessUnhandledInput(true);
            BuildUi();
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

        private void BuildUi()
        {
            var backdrop = new ColorRect
            {
                Color = new Color(0f, 0f, 0f, 0.62f),
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

            var outer = new PanelContainer
            {
                CustomMinimumSize = new Vector2(1320f, 1080f),
                MouseFilter = MouseFilterEnum.Stop,
                SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
                SizeFlagsVertical = SizeFlags.ShrinkCenter
            };
            outer.AddThemeStyleboxOverride("panel", CreatePanelStyle(new Color(0.09f, 0.11f, 0.16f, 0.98f), 20));
            center.AddChild(outer);

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 24);
            margin.AddThemeConstantOverride("margin_top", 22);
            margin.AddThemeConstantOverride("margin_right", 24);
            margin.AddThemeConstantOverride("margin_bottom", 18);
            outer.AddChild(margin);

            var root = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            root.AddThemeConstantOverride("separation", 16);
            margin.AddChild(root);

            root.AddChild(BuildHeader());
            root.AddChild(BuildComponentList());
        }

        private Control BuildHeader()
        {
            var row = new HBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            row.AddThemeConstantOverride("separation", 14);

            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(52f, 52f),
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore,
                Texture = ResourceLoader.Load<Texture2D>(IconPath)
            };
            row.AddChild(icon);

            var titleBox = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            titleBox.AddThemeConstantOverride("separation", 4);
            row.AddChild(titleBox);

            titleBox.AddChild(CreateLabel(
                new LocString("relics", $"{HoverTipRoot}.title").GetRawText(),
                28,
                new Color(0.98f, 0.89f, 0.53f)));
            titleBox.AddChild(CreateLabel(
                "右键列表展示全部畸变组件。",
                15,
                new Color(0.84f, 0.87f, 0.93f, 0.92f)));

            var close = new TextureButton
            {
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(119f, 67f),
                TextureNormal = ResourceLoader.Load<Texture2D>(CloseButtonPath),
                TextureHover = ResourceLoader.Load<Texture2D>(CloseButtonPath),
                TexturePressed = ResourceLoader.Load<Texture2D>(CloseButtonPath),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            close.Pressed += Close;
            row.AddChild(close);
            DefaultFocusedControl = close;

            return row;
        }

        private Control BuildComponentList()
        {
            var scroll = new ScrollContainer
            {
                MouseFilter = MouseFilterEnum.Pass,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto
            };

            scroll.AddChild(FinalArenaDistortionComponentCardUi.BuildComponentGrid(_components, _activeComponents));
            return scroll;
        }

        private static Label CreateLabel(string text, int fontSize, Color color, bool wrap = false)
        {
            var label = new Label
            {
                Text = text,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            if (wrap)
                label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            var displayFont = ResourceLoader.Load<Font>(FinalArenaDistortionComponentCardUi.FontPath);
            if (displayFont != null)
                label.AddThemeFontOverride("font", displayFont);
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static StyleBoxFlat CreatePanelStyle(Color bg, int radius)
        {
            var style = new StyleBoxFlat
            {
                BgColor = bg,
                BorderColor = new Color(0.38f, 0.47f, 0.64f, 0.92f),
                CornerRadiusTopLeft = radius,
                CornerRadiusTopRight = radius,
                CornerRadiusBottomRight = radius,
                CornerRadiusBottomLeft = radius,
                ShadowColor = new Color(0f, 0f, 0f, 0.24f),
                ShadowSize = 10
            };
            style.BorderWidthLeft = 2;
            style.BorderWidthTop = 2;
            style.BorderWidthRight = 2;
            style.BorderWidthBottom = 2;
            return style;
        }

        private static void Close()
        {
            NModalContainer.Instance?.Clear();
        }
    }
}
