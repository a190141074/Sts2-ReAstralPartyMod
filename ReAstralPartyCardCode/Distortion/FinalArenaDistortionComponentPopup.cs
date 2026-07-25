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
    private const string HoverTipRoot = "RE_ASTRAL_PARTY_MOD_DISTORTION_COMPONENT";

    public static void Open(IRunState runState)
    {
        if (!FinalArenaSeriesHelper.TryGetActiveComponents(runState, out var components) || components.Count == 0)
            return;
        if (NModalContainer.Instance == null)
            return;

        NModalContainer.Instance.Clear();
        NModalContainer.Instance.Add(new FinalArenaDistortionComponentPopupPanel(components));
        NModalContainer.Instance.ShowBackstop();
    }

    private sealed partial class FinalArenaDistortionComponentPopupPanel : Control, IScreenContext
    {
        private readonly IReadOnlyList<FinalArenaDistortionComponent> _components;

        public FinalArenaDistortionComponentPopupPanel(IReadOnlyList<FinalArenaDistortionComponent> components)
        {
            _components = components;
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
                CustomMinimumSize = new Vector2(980f, 620f),
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
            root.AddChild(BuildSummaryCard());
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
                "右键列表展示当前已激活的全部畸变组件。",
                15,
                new Color(0.84f, 0.87f, 0.93f, 0.92f)));

            var close = new Button
            {
                Text = "关闭",
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                CustomMinimumSize = new Vector2(124f, 42f),
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd
            };
            close.Pressed += Close;
            row.AddChild(close);
            DefaultFocusedControl = close;

            return row;
        }

        private Control BuildSummaryCard()
        {
            var body = new LocString("relics", $"{HoverTipRoot}.description").GetRawText();
            return BuildCard(
                new LocString("relics", $"{HoverTipRoot}.title").GetRawText(),
                body,
                new Color(0.2f, 0.26f, 0.38f, 0.98f));
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

            var list = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                SizeFlagsVertical = SizeFlags.ExpandFill
            };
            list.AddThemeConstantOverride("separation", 12);
            scroll.AddChild(list);

            foreach (var component in _components)
            {
                var title = new LocString("relics",
                    $"{HoverTipRoot}.{FinalArenaSeriesHelper.GetLocStem(component)}.title");
                title.Add("Difficulty", FinalArenaSeriesHelper.GetDifficulty(component));
                var description = new LocString("relics",
                    $"{HoverTipRoot}.{FinalArenaSeriesHelper.GetLocStem(component)}.description").GetRawText();
                list.AddChild(BuildCard(title.GetRawText(), description, new Color(0.13f, 0.17f, 0.24f, 0.98f)));
            }

            return scroll;
        }

        private static Control BuildCard(string title, string description, Color background)
        {
            var card = new PanelContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                CustomMinimumSize = new Vector2(0f, 0f)
            };
            card.AddThemeStyleboxOverride("panel", CreatePanelStyle(background, 14));

            var margin = new MarginContainer
            {
                MouseFilter = MouseFilterEnum.Ignore
            };
            margin.AddThemeConstantOverride("margin_left", 16);
            margin.AddThemeConstantOverride("margin_top", 14);
            margin.AddThemeConstantOverride("margin_right", 16);
            margin.AddThemeConstantOverride("margin_bottom", 14);
            card.AddChild(margin);

            var box = new VBoxContainer
            {
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            box.AddThemeConstantOverride("separation", 6);
            margin.AddChild(box);

            box.AddChild(CreateLabel(title, 20, new Color(0.97f, 0.94f, 0.86f)));
            box.AddChild(CreateLabel(description, 16, new Color(0.86f, 0.88f, 0.93f, 0.96f), true));
            return card;
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
