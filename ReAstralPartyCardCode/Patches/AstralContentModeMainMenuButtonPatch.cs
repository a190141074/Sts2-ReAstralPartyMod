using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using ReAstralPartyMod.ReAstralPartyCardCode.Screens;
using ReAstralPartyMod.ReAstralPartyCardCode.Server;
using STS2RitsuLib.Patching.Models;
using STS2RitsuLib.Scaffolding.Godot;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed partial class AstralContentModeMainMenuButtonPatch : IPatchMethod
{
    private const string RitsuLibSettingsGroupNodeName = "RitsuLibMainMenuModSettings";
    private const string GroupNodeName = "AstralContentModeMainMenuButtonGroup";
    private const string ButtonNodeName = "AstralContentModeMainMenuButton";
    private const string VersionLabelNodeName = "AstralContentModeMainMenuButtonVersion";
    private const string VisibilitySyncNodeName = "AstralContentModeMainMenuButtonVisibilitySync";
    private const string IconPath = "res://ReAstralPartyMod/images/ui/global_switching_settings.png";
    private const string HsvShaderPath = "res://shaders/hsv.gdshader";
    private const float ButtonSize = 64f;
    private const float GapBelowPatchNotes = 8f;
    private const float VersionLabelTop = 2f;
    private const float VersionLabelHeight = 56f;
    private const float VersionLabelLeft = -218f;
    private const float VersionLabelRight = -6f;

    public static string PatchId => "astral_content_mode_main_menu_button";
    public static string Description => "Add the Astral content mode shortcut under the main menu patch notes button";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(NMainMenu), nameof(NMainMenu._Ready)),
            new(typeof(NMainMenu), "OnSubmenuStackChanged"),
        ];
    }

    public static void Postfix(NMainMenu __instance)
    {
        try
        {
            var group = EnsureEntry(__instance);
            SyncState(__instance, group);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[{PatchId}] Failed to add main menu content-mode shortcut: {ex}");
        }
    }

    private static Control? EnsureEntry(NMainMenu mainMenu)
    {
        if (!GodotObject.IsInstanceValid(mainMenu))
            return null;

        var anchorControl = ResolveAnchorControl(mainMenu);
        if (anchorControl == null || !GodotObject.IsInstanceValid(anchorControl))
            return null;

        if (mainMenu.GetNodeOrNull<Control>(GroupNodeName) is { } existing)
        {
            SyncPlacement(existing, anchorControl);
            ApplyReleaseInfoTypography(existing, mainMenu);
            RefreshVersionLabel(existing);
            EnsureVisibilitySynchronizer(existing, mainMenu);
            return existing;
        }

        var group = new Control
        {
            Name = GroupNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(ButtonSize, ButtonSize)
        };

        var button = AstralContentModeMainMenuButton.Create();
        button.Name = ButtonNodeName;
        button.Connect(
            NClickableControl.SignalName.Released,
            Callable.From<NClickableControl>(_ => OpenContentModeSwitch(mainMenu)));
        group.AddChild(button);
        group.AddChild(CreateVersionLabel());

        RitsuGodotTreeCompat.AddChildSafely(mainMenu, group);
        SyncPlacement(group, anchorControl);
        ApplyReleaseInfoTypography(group, mainMenu);
        EnsureVisibilitySynchronizer(group, mainMenu);
        return group;
    }

    private static void EnsureVisibilitySynchronizer(Control group, NMainMenu mainMenu)
    {
        if (group.GetNodeOrNull<AstralContentModeMainMenuVisibilitySync>(VisibilitySyncNodeName) is { } existing)
        {
            existing.Configure(mainMenu, group);
            return;
        }

        var sync = new AstralContentModeMainMenuVisibilitySync
        {
            Name = VisibilitySyncNodeName
        };
        sync.Configure(mainMenu, group);
        group.AddChild(sync);
    }

    private static void SyncPlacement(Control group, Control anchorControl)
    {
        group.AnchorLeft = anchorControl.AnchorLeft;
        group.AnchorTop = anchorControl.AnchorTop;
        group.AnchorRight = anchorControl.AnchorRight;
        group.AnchorBottom = anchorControl.AnchorBottom;
        group.GrowHorizontal = anchorControl.GrowHorizontal;
        group.GrowVertical = anchorControl.GrowVertical;
        group.OffsetLeft = anchorControl.OffsetLeft;
        group.OffsetRight = anchorControl.OffsetRight;
        group.OffsetTop = anchorControl.OffsetBottom + GapBelowPatchNotes;
        group.OffsetBottom = group.OffsetTop + ButtonSize;

        if (group.GetNodeOrNull<Control>(ButtonNodeName) is not { } button)
            goto VersionLabelPlacement;

        button.OffsetLeft = 0f;
        button.OffsetTop = 0f;
        button.OffsetRight = ButtonSize;
        button.OffsetBottom = ButtonSize;

VersionLabelPlacement:
        if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is not { } label)
            return;

        label.OffsetLeft = VersionLabelLeft;
        label.OffsetTop = VersionLabelTop;
        label.OffsetRight = VersionLabelRight;
        label.OffsetBottom = VersionLabelTop + VersionLabelHeight;
    }

    internal static void SyncState(NMainMenu mainMenu, Control? group)
    {
        if (group == null || !GodotObject.IsInstanceValid(group))
            return;

        var shouldShow = IsMainMenuShortcutSurfaceVisible(mainMenu);
        group.Visible = shouldShow;
        if (group.GetNodeOrNull<AstralContentModeMainMenuButton>(ButtonNodeName) is { } button)
            button.SetEnabled(shouldShow);
    }

    private static bool IsMainMenuShortcutSurfaceVisible(NMainMenu mainMenu)
    {
        if (!GodotObject.IsInstanceValid(mainMenu) || mainMenu.SubmenuStack.SubmenusOpen)
            return false;

        if (mainMenu.GetNodeOrNull<Control>("%PatchNotesButton") is not { } patchNotesButton
            || !GodotObject.IsInstanceValid(patchNotesButton)
            || !patchNotesButton.Visible)
            return false;

        return mainMenu.PatchNotesScreen is not { } patchNotesScreen
               || !GodotObject.IsInstanceValid(patchNotesScreen)
               || (!patchNotesScreen.IsOpen && !patchNotesScreen.Visible);
    }

    private static Control? ResolveAnchorControl(NMainMenu mainMenu)
    {
        if (mainMenu.GetNodeOrNull<Control>(RitsuLibSettingsGroupNodeName) is { } ritsuGroup
            && GodotObject.IsInstanceValid(ritsuGroup))
            return ritsuGroup;

        return mainMenu.GetNodeOrNull<Control>("%PatchNotesButton");
    }

    private static void OpenContentModeSwitch(NMainMenu mainMenu)
    {
        if (!GodotObject.IsInstanceValid(mainMenu))
            return;

        AstralContentModeMainMenuPopup.Open();
    }

    private static Label CreateVersionLabel()
    {
        var label = new Label
        {
            Name = VersionLabelNodeName,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        label.AddThemeColorOverride("font_color", new Color(0.91f, 0.86359f, 0.7462f));
        label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.72f));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeFontSizeOverride("font_size", 16);
        RefreshVersionLabel(label);
        return label;
    }

    private static void ApplyReleaseInfoTypography(Control group, NMainMenu mainMenu)
    {
        if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is not { } label
            || mainMenu.GetNodeOrNull<Label>("%ReleaseInfo") is not { } releaseInfo)
            return;

        label.AddThemeFontOverride("font", releaseInfo.GetThemeFont("font"));
        label.AddThemeFontSizeOverride("font_size", releaseInfo.GetThemeFontSize("font_size"));
    }

    private static void RefreshVersionLabel(Control group)
    {
        if (group.GetNodeOrNull<Label>(VersionLabelNodeName) is { } label)
            RefreshVersionLabel(label);
    }

    private static void RefreshVersionLabel(Label label)
    {
        label.Text = $"模式切换\nv{AstralContentModeMainMenuVersionHelper.GetModVersion()}";
    }

    internal sealed partial class AstralContentModeMainMenuVisibilitySync : Node
    {
        private Control? _group;
        private NMainMenu? _mainMenu;

        public void Configure(NMainMenu mainMenu, Control group)
        {
            _mainMenu = mainMenu;
            _group = group;
        }

        public override void _Process(double delta)
        {
            if (_mainMenu == null || _group == null || !IsInstanceValid(_mainMenu) || !IsInstanceValid(_group))
            {
                QueueFree();
                return;
            }

            SyncState(_mainMenu, _group);
        }
    }

    internal sealed partial class AstralContentModeMainMenuButton : NButton
    {
        private static readonly StringName ShaderParamV = new("v");
        private Control? _icon;
        private ShaderMaterial? _hsv;

        public static AstralContentModeMainMenuButton Create()
        {
            var button = new AstralContentModeMainMenuButton
            {
                CustomMinimumSize = new Vector2(ButtonSize, ButtonSize),
                FocusMode = FocusModeEnum.All,
                MouseFilter = MouseFilterEnum.Stop,
                PivotOffset = new Vector2(ButtonSize / 2f, ButtonSize / 2f)
            };

            var icon = new TextureRect
            {
                Name = "Icon",
                Material = CreateHsvMaterial(),
                AnchorRight = 1f,
                AnchorBottom = 1f,
                GrowHorizontal = GrowDirection.Both,
                GrowVertical = GrowDirection.Both,
                PivotOffset = button.PivotOffset,
                MouseFilter = MouseFilterEnum.Ignore,
                Texture = ResourceLoader.Load<Texture2D>(IconPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            button.AddChild(icon);
            return button;
        }

        public override void _Ready()
        {
            ConnectSignals();
            _icon = GetNode<Control>("Icon");
            _hsv = _icon.Material as ShaderMaterial;
        }

        protected override void OnFocus()
        {
            base.OnFocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1.2f);
            if (_icon != null)
                _icon.RotationDegrees = 5f;
        }

        protected override void OnUnfocus()
        {
            base.OnUnfocus();
            _hsv?.SetShaderParameter(ShaderParamV, 1f);
            if (_icon != null)
                _icon.RotationDegrees = 0f;
        }

        private static ShaderMaterial CreateHsvMaterial()
        {
            var material = new ShaderMaterial
            {
                ResourceLocalToScene = true,
                Shader = ResourceLoader.Load<Shader>(HsvShaderPath)
            };
            material.SetShaderParameter("h", 1f);
            material.SetShaderParameter("s", 1f);
            material.SetShaderParameter(ShaderParamV, 1f);
            return material;
        }
    }
}
