using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
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

        MainFile.Logger.Info($"Top bar open token series patch active | {TokenSeriesAvailabilityHelper.BuildDebugSummary(runState)}");

        var existing = __instance.GetNodeOrNull<Control>(NodeName);
        existing?.QueueFree();

        var modifiers = __instance.GetNodeOrNull<Control>("%Modifiers");
        if (modifiers == null)
        {
            MainFile.Logger.Warn("Top bar open token series icon skipped because %Modifiers container was not found.");
            return;
        }

        var iconRoot = new TokenSeriesTopBarIcon(runState)
        {
            Name = NodeName
        };
        modifiers.AddChild(iconRoot);
        modifiers.Visible = true;
    }

    private sealed partial class TokenSeriesTopBarIcon : Control
    {
        private readonly IRunState _runState;
        private IReadOnlyList<IHoverTip> _hoverTips = [];

        public TokenSeriesTopBarIcon(IRunState runState)
        {
            _runState = runState;
            CustomMinimumSize = new Vector2(56f, 80f);
            Size = new Vector2(56f, 80f);
            MouseFilter = MouseFilterEnum.Stop;
        }

        public override void _Ready()
        {
            var textureRect = new TextureRect
            {
                Name = "Icon",
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
                Texture = ResourceLoader.Load<Texture2D>("res://ReAstralPartyMod/images/astral_token.png")
            };
            AddChild(textureRect);

            _hoverTips = TokenSeriesAvailabilityHelper.BuildHoverTips(_runState, textureRect.Texture);

            Connect(Control.SignalName.MouseEntered, Callable.From(OnMouseEntered));
            Connect(Control.SignalName.MouseExited, Callable.From(OnMouseExited));
        }

        private void OnMouseEntered()
        {
            if (_hoverTips.Count == 0)
                return;

            var tip = NHoverTipSet.CreateAndShow(this, _hoverTips);
            tip.GlobalPosition = GlobalPosition + new Vector2(0f, Size.Y + 20f);
        }

        private void OnMouseExited()
        {
            NHoverTipSet.Remove(this);
        }
    }
}
