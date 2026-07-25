using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Distortion;
using ReAstralPartyMod.ReAstralPartyCardCode.Modifiers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static partial class NTopBarOpenTokenSeriesPatch
{
    private const string NodeName = "ReAstralPartyMod_OpenTokenSeriesIcon";

    [HarmonyPostfix]
    public static void Postfix(NTopBar __instance, IRunState runState)
    {
        if (!TokenSeriesAvailabilityHelper.TryGetState(runState, out _))
            return;

        MainFile.Logger.Info(
            $"Top bar open token series patch active | {TokenSeriesAvailabilityHelper.BuildDebugSummary(runState)}");

        var existing = __instance.GetNodeOrNull<Control>(NodeName);
        existing?.QueueFree();

        var modifiers = __instance.GetNodeOrNull<Control>("%Modifiers");
        if (modifiers == null)
        {
            MainFile.Logger.Warn("Top bar open token series icon skipped because %Modifiers container was not found.");
            return;
        }

        HideAstralRelicStoreModifierIcons(modifiers, runState);

        var iconRoot = new TokenSeriesTopBarIcon(runState)
        {
            Name = NodeName
        };
        modifiers.AddChild(iconRoot);
        modifiers.Visible = true;
    }

    private static void HideAstralRelicStoreModifierIcons(Control modifiers, IRunState runState)
    {
        if (!runState.Modifiers.Any(static modifier => modifier is AstralRelicStoreFirstEventModifier))
            return;

        var hiddenCount = 0;
        foreach (var child in modifiers.GetChildren())
        {
            if (child is not Control control)
                continue;
            if (control.Name == NodeName)
                continue;

            control.Visible = false;
            hiddenCount++;
        }

        if (hiddenCount > 0)
            MainFile.Logger.Info($"Top bar hid {hiddenCount} modifier icon node(s) for AstralRelicStoreFirstEventModifier.");
    }

    private sealed partial class TokenSeriesTopBarIcon : Control
    {
        private readonly IRunState _runState;
        private Texture2D? _seriesIcon;
        private Texture2D? _distortionIcon;

        public TokenSeriesTopBarIcon(IRunState runState)
        {
            _runState = runState;
            var hasDistortionIcon = FinalArenaSeriesHelper.TryGetActiveComponents(_runState, out _);
            var width = hasDistortionIcon ? 112f : 56f;
            CustomMinimumSize = new Vector2(width, 80f);
            Size = new Vector2(width, 80f);
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Ready()
        {
            AddChild(CreateIconNode(
                "SeriesIcon",
                0f,
                ResourceLoader.Load<Texture2D>("res://ReAstralPartyMod/images/astral_token.png"),
                onMouseEntered: OnSeriesMouseEntered,
                onMouseExited: OnMouseExited));

            if (FinalArenaSeriesHelper.TryGetActiveComponents(_runState, out _))
            {
                AddChild(CreateIconNode(
                    "DistortionIcon",
                    56f,
                    ResourceLoader.Load<Texture2D>("res://ReAstralPartyMod/images/ui/distortion_component_logo.png"),
                    onMouseEntered: OnDistortionMouseEntered,
                    onMouseExited: OnMouseExited));
            }
        }

        private Control CreateIconNode(
            string name,
            float left,
            Texture2D? texture,
            Action onMouseEntered,
            Action onMouseExited)
        {
            var iconRoot = new Control
            {
                Name = name,
                Position = new Vector2(left, 0f),
                Size = new Vector2(56f, 80f),
                CustomMinimumSize = new Vector2(56f, 80f),
                MouseFilter = MouseFilterEnum.Stop
            };
            var textureRect = new TextureRect
            {
                Name = $"{name}_Texture",
                CustomMinimumSize = new Vector2(48f, 48f),
                MouseFilter = MouseFilterEnum.Ignore,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                AnchorLeft = 0.5f,
                AnchorTop = 0.5f,
                AnchorRight = 0.5f,
                AnchorBottom = 0.5f,
                OffsetLeft = -24f,
                OffsetTop = -27f,
                OffsetRight = 24f,
                OffsetBottom = 21f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                Texture = texture
            };
            iconRoot.AddChild(textureRect);
            iconRoot.Connect(Control.SignalName.MouseEntered, Callable.From(onMouseEntered));
            iconRoot.Connect(Control.SignalName.MouseExited, Callable.From(onMouseExited));

            if (name == "SeriesIcon")
                _seriesIcon = texture;
            else
                _distortionIcon = texture;

            return iconRoot;
        }

        private void OnSeriesMouseEntered()
        {
            ShowHoverTips(TokenSeriesAvailabilityHelper.BuildHoverTips(_runState, _seriesIcon), 0f);
        }

        private void OnDistortionMouseEntered()
        {
            ShowHoverTips(FinalArenaSeriesHelper.BuildDistortionHoverTips(_runState, _distortionIcon), 56f);
        }

        private void OnMouseExited()
        {
            NHoverTipSet.Remove(this);
        }

        private void ShowHoverTips(IReadOnlyList<IHoverTip> hoverTips, float xOffset)
        {
            if (hoverTips.Count == 0)
                return;

            var tip = NHoverTipSet.CreateAndShow(this, hoverTips);
            if (tip == null)
                return;

            tip.GlobalPosition = GlobalPosition + new Vector2(xOffset, Size.Y + 20f);
        }
    }
}
