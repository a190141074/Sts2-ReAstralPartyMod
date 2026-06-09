using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

internal sealed partial class CharacterSelectLucidDreamMalicePanel : Control
{
    private enum LucidDreamPanelGroup
    {
        Benevolence = 0,
        Malice = 1,
        Chaos = 2
    }

    private enum LucidDreamSettingKey
    {
        FalseLifeline = 0,
        SmoothSailing = 1,
        FishScales = 2,
        SevereWoundOne = 3,
        SevereWoundTwo = 4,
        MadLife = 5,
        SwampOfFate = 6,
        Overpopulation = 7,
        CautiousJellyfish = 8,
        FaceDeathWithComposure = 9,
        Wildness = 10,
        PitchBlackImpulse = 11,
        BubblePotionOfDreams = 12,
        HarmlessWhisper = 13
    }

    private sealed record LucidDreamEntry(
        LucidDreamPanelGroup Group,
        LucidDreamSettingKey Key,
        string TexturePath,
        string TitleKey,
        string DescriptionKey);

    private sealed record LucidDreamGroupDefinition(
        LucidDreamPanelGroup Group,
        string TitleKey,
        bool MarkUnfinished,
        LucidDreamEntry[] Entries);

    private sealed class LucidDreamToggleView
    {
        public required Button Button { get; init; }
        public required Label CheckLabel { get; init; }
        public required LucidDreamEntry Entry { get; init; }
    }

    private const float LayoutScale = 0.85f;
    private const float BasePanelWidth = 470f;
    private const float BasePanelHeight = 168f;
    private const float BasePanelViewportMargin = 36f;
    private const float BaseIconSize = 51f;
    private const float BaseIconButtonWidth = 60f;
    private const float BaseIconButtonHeight = 82f;
    private const float BasePanelSpacing = 10f;
    private const float BaseTitleRowSpacing = 8f;
    private const float BaseGroupContentSpacing = 8f;
    private const float BaseIconRowSpacing = 10f;
    private const float BaseIconContentSpacing = 2f;
    private const float BaseShellMarginHorizontal = 18f;
    private const float BaseShellMarginVertical = 10f;
    private const float BaseShadowOffsetX = 8f;
    private const float BaseShadowOffsetY = 10f;
    private const float BaseIconButtonContentMargin = 2f;
    private const float PanelWidth = BasePanelWidth * LayoutScale;
    private const float PanelHeight = BasePanelHeight * LayoutScale;
    private const float PanelViewportMargin = BasePanelViewportMargin * LayoutScale;
    private const float IconSize = BaseIconSize * LayoutScale;
    private const float IconButtonWidth = BaseIconButtonWidth * LayoutScale;
    private const float IconButtonHeight = BaseIconButtonHeight * LayoutScale;
    private const float PanelSpacing = BasePanelSpacing * LayoutScale;
    private const float TitleRowSpacing = BaseTitleRowSpacing * LayoutScale;
    private const float GroupContentSpacing = BaseGroupContentSpacing * LayoutScale;
    private const float IconRowSpacing = BaseIconRowSpacing * LayoutScale;
    private const float IconContentSpacing = BaseIconContentSpacing * LayoutScale;
    private const float ShellMarginHorizontal = BaseShellMarginHorizontal * LayoutScale;
    private const float ShellMarginVertical = BaseShellMarginVertical * LayoutScale;
    private const float ShadowOffsetX = BaseShadowOffsetX * LayoutScale;
    private const float ShadowOffsetY = BaseShadowOffsetY * LayoutScale;
    private const float IconButtonContentMargin = BaseIconButtonContentMargin * LayoutScale;
    private const float PanelLeftShift = (IconButtonWidth + IconRowSpacing) * 2f;
    private const float TitleFontSize = 18f;
    private const float UnfinishedFontSize = 15f;

    private static readonly LucidDreamEntry[] BenevolenceEntries =
    [
        new(
            LucidDreamPanelGroup.Benevolence,
            LucidDreamSettingKey.FalseLifeline,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_false_lifeline.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.false_lifeline.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.false_lifeline.description"),
        new(
            LucidDreamPanelGroup.Benevolence,
            LucidDreamSettingKey.SmoothSailing,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_smooth_sailing.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.smooth_sailing.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.smooth_sailing.description")
    ];

    private static readonly LucidDreamEntry[] MaliceEntries =
    [
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.FishScales,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_fish_scales.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.SevereWoundOne,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_severe_wound_one.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.SevereWoundTwo,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_severe_wound_two.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.MadLife,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_mad_life.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.SwampOfFate,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_swamp_of_fate.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.Overpopulation,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_overpopulation.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.description"),
        new(
            LucidDreamPanelGroup.Malice,
            LucidDreamSettingKey.CautiousJellyfish,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_cautious_jellyfish.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.description")
    ];

    private static readonly LucidDreamEntry[] ChaosEntries =
    [
        new(
            LucidDreamPanelGroup.Chaos,
            LucidDreamSettingKey.FaceDeathWithComposure,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_face_death_with_composure.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.face_death_with_composure.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.face_death_with_composure.description"),
        new(
            LucidDreamPanelGroup.Chaos,
            LucidDreamSettingKey.Wildness,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_wildness.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.wildness.description"),
        new(
            LucidDreamPanelGroup.Chaos,
            LucidDreamSettingKey.PitchBlackImpulse,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_pitch_black_impulse.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.pitch_black_impulse.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.pitch_black_impulse.description"),
        new(
            LucidDreamPanelGroup.Chaos,
            LucidDreamSettingKey.BubblePotionOfDreams,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_bubble_potion_of_dreams.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.bubble_potion_of_dreams.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.bubble_potion_of_dreams.description"),
        new(
            LucidDreamPanelGroup.Chaos,
            LucidDreamSettingKey.HarmlessWhisper,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_harmless_whisper.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.harmless_whisper.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.harmless_whisper.description")
    ];

    private static readonly LucidDreamGroupDefinition[] Groups =
    [
        new(
            LucidDreamPanelGroup.Benevolence,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_benevolence.title",
            false,
            BenevolenceEntries),
        new(
            LucidDreamPanelGroup.Malice,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.title",
            false,
            MaliceEntries),
        new(
            LucidDreamPanelGroup.Chaos,
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_chaos.title",
            false,
            ChaosEntries)
    ];

    private readonly Dictionary<Control, IReadOnlyList<IHoverTip>> _hoverTipsByControl = [];
    private readonly List<LucidDreamToggleView> _toggleViews = [];
    private readonly List<PanelContainer> _groupPanels = [];
    private Godot.Timer? _refreshTimer;
    private Vector2 _lastViewportSize = Vector2.Zero;
    private bool _handlersBound;
    private LobbyGameplayNetRole _lastKnownRole = LobbyGameplayNetRole.Pending;
    private INetGameService? _lastObservedNetService;
    private StartRunLobby? _lastObservedLobby;

    public CharacterSelectLucidDreamMalicePanel()
    {
        MouseFilter = MouseFilterEnum.Pass;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        BuildUi();
        PositionPanel();
        BuildRefreshTimer();
        LobbyGameplaySettingsSync.SnapshotChanged += OnSnapshotChanged;
        _handlersBound = true;
        RefreshRoleState();
        RefreshFromCurrentState();
    }

    public override void _ExitTree()
    {
        if (_handlersBound)
            LobbyGameplaySettingsSync.SnapshotChanged -= OnSnapshotChanged;
        _handlersBound = false;
        ClearAllHoverTips();
    }

    public override void _Process(double delta)
    {
        var viewportSize = GetViewportRect().Size;
        if (!viewportSize.IsEqualApprox(_lastViewportSize))
        {
            _lastViewportSize = viewportSize;
            PositionPanel();
        }
    }

    private void BuildUi()
    {
        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.Fill,
            SizeFlagsVertical = SizeFlags.Fill
        };
        root.AddThemeConstantOverride("separation", (int)PanelSpacing);
        AddChild(root);

        foreach (var group in Groups)
            root.AddChild(BuildGroupPanel(group));
    }

    private Control BuildGroupPanel(LucidDreamGroupDefinition group)
    {
        var wrapper = new Control
        {
            CustomMinimumSize = new Vector2(PanelWidth, PanelHeight),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.Fill,
            MouseFilter = MouseFilterEnum.Pass
        };

        var shadow = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.28f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        shadow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shadow.OffsetLeft = ShadowOffsetX;
        shadow.OffsetTop = ShadowOffsetY;
        shadow.OffsetRight = ShadowOffsetX;
        shadow.OffsetBottom = ShadowOffsetY;
        wrapper.AddChild(shadow);

        var shell = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        shell.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shell.AddThemeStyleboxOverride("panel", CreateLucidDreamShellStyle());
        wrapper.AddChild(shell);
        _groupPanels.Add(shell);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.AddThemeConstantOverride("separation", (int)MathF.Round(GroupContentSpacing));
        shell.AddChild(root);

        var titleCenter = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        titleCenter.AddChild(BuildTitleRow(group));
        root.AddChild(titleCenter);

        var rowCenter = new CenterContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        root.AddChild(rowCenter);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        row.AddThemeConstantOverride("separation", (int)MathF.Round(IconRowSpacing));
        rowCenter.AddChild(row);

        for (var index = 0; index < group.Entries.Length; index++)
            row.AddChild(BuildIconToggle(group.Entries[index], index));

        return wrapper;
    }

    private static Control BuildTitleRow(LucidDreamGroupDefinition group)
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", (int)MathF.Round(TitleRowSpacing));

        var title = new Label
        {
            Text = GetText(group.TitleKey),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 0.96f));
        title.AddThemeFontSizeOverride("font_size", (int)TitleFontSize);
        row.AddChild(title);

        if (group.MarkUnfinished)
        {
            var unfinished = new Label
            {
                Text = GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream.unfinished_suffix"),
                HorizontalAlignment = HorizontalAlignment.Center,
                MouseFilter = MouseFilterEnum.Ignore
            };
            unfinished.AddThemeColorOverride("font_color", new Color(0.9f, 0.28f, 0.28f, 0.98f));
            unfinished.AddThemeFontSizeOverride("font_size", (int)UnfinishedFontSize);
            row.AddChild(unfinished);
        }

        return row;
    }

    private Control BuildIconToggle(LucidDreamEntry entry, int index)
    {
        var button = new Button
        {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None,
            Flat = true,
            CustomMinimumSize = new Vector2(IconButtonWidth, IconButtonHeight),
            TooltipText = string.Empty
        };
        button.AddThemeStyleboxOverride("normal", CreateIconButtonStyle(false));
        button.AddThemeStyleboxOverride("hover", CreateIconButtonStyle(true));
        button.AddThemeStyleboxOverride("pressed", CreateIconButtonStyle(true));
        button.AddThemeStyleboxOverride("disabled", CreateIconButtonStyle(false));

        var content = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        content.AddThemeConstantOverride("separation", (int)MathF.Round(IconContentSpacing));
        button.AddChild(content);

        var checkLabel = new Label
        {
            Text = "✓",
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            Visible = false
        };
        checkLabel.AddThemeColorOverride("font_color", new Color(0.35f, 0.98f, 0.58f, 1f));
        checkLabel.AddThemeFontSizeOverride("font_size", 18);
        content.AddChild(checkLabel);

        var iconTexture = new TextureRect
        {
            Texture = ResourceLoader.Load<Texture2D>(entry.TexturePath),
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            MouseFilter = MouseFilterEnum.Ignore
        };
        content.AddChild(iconTexture);

        var view = new LucidDreamToggleView
        {
            Button = button,
            CheckLabel = checkLabel,
            Entry = entry
        };
        _toggleViews.Add(view);
        RegisterHover(button, entry);
        button.Pressed += () => OnTogglePressed(entry.Key);
        button.Name = $"LucidDreamIcon{entry.Group}_{index}";
        return button;
    }

    private void BuildRefreshTimer()
    {
        _refreshTimer = new Godot.Timer
        {
            WaitTime = 0.25d,
            Autostart = true,
            OneShot = false,
            ProcessCallback = Godot.Timer.TimerProcessCallback.Idle
        };
        _refreshTimer.Timeout += OnRefreshTimerTimeout;
        AddChild(_refreshTimer);
    }

    private void RefreshRoleState()
    {
        var lobby = GetCharacterSelectLobby();
        var netService = lobby?.NetService ?? RunManager.Instance?.NetService;
        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(lobby);
        var lobbyChanged = !ReferenceEquals(_lastObservedLobby, lobby);
        var netServiceChanged = !ReferenceEquals(_lastObservedNetService, netService);
        var roleChanged = role != _lastKnownRole;

        if (lobbyChanged)
            _lastObservedLobby = lobby;

        if (netServiceChanged)
            _lastObservedNetService = netService;

        if (roleChanged || lobbyChanged || netServiceChanged)
        {
            _lastKnownRole = role;
            RefreshFromCurrentState();
        }
    }

    private void RefreshFromCurrentState()
    {
        var role = GetCurrentRoleForUi();
        var snapshot = LobbyGameplaySettingsSync.TryGetSnapshot(out var current)
            ? current
            : role == LobbyGameplayNetRole.Host
                ? LobbyGameplaySettingsSync.BuildFallbackSnapshot()
                : LobbyGameplaySettingsSync.BuildDefaultSnapshot();
        ApplySnapshot(snapshot);
    }

    private void ApplySnapshot(LobbyGameplaySettingsSnapshot snapshot)
    {
        var isEditable = GetCurrentRoleForUi() == LobbyGameplayNetRole.Host;
        foreach (var toggleView in _toggleViews)
        {
            var isEnabled = GetSettingValue(snapshot, toggleView.Entry.Key);
            toggleView.CheckLabel.Visible = isEnabled;
            toggleView.Button.Modulate = isEditable
                ? Colors.White
                : new Color(1f, 1f, 1f, 0.85f);
            toggleView.Button.Disabled = false;
        }
    }

    private void OnSnapshotChanged(LobbyGameplaySettingsSnapshot snapshot)
    {
        CallDeferred(nameof(RefreshDeferred));
    }

    private void RefreshDeferred()
    {
        RefreshFromCurrentState();
    }

    private void OnTogglePressed(LucidDreamSettingKey key)
    {
        if (GetCurrentRoleForUi() != LobbyGameplayNetRole.Host)
            return;

        var currentSnapshot = LobbyGameplaySettingsSync.TryGetSnapshot(out var snapshot)
            ? snapshot
            : LobbyGameplaySettingsSync.BuildFallbackSnapshot();
        var nextValue = !GetSettingValue(currentSnapshot, key);
        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshotToMutate => SetSettingValue(snapshotToMutate, key, nextValue));
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnRefreshTimerTimeout()
    {
        RefreshRoleState();
    }

    private void PositionPanel()
    {
        if (GetChildCount() == 0 || GetChild(0) is not Control root)
            return;

        var viewportSize = GetViewportRect().Size;
        var totalHeight = GetCombinedPanelHeight();
        var x = Mathf.Clamp(
            viewportSize.X - PanelWidth - 96f - PanelLeftShift,
            PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportSize.X - PanelWidth - PanelViewportMargin));
        var y = Mathf.Clamp(
            (viewportSize.Y - totalHeight) * 0.5f,
            PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportSize.Y - totalHeight - PanelViewportMargin));

        root.Position = new Vector2(x, y);
        root.Size = new Vector2(PanelWidth, totalHeight);
    }

    private float GetCombinedPanelHeight()
    {
        var height = 0f;
        for (var index = 0; index < Groups.Length; index++)
        {
            height += PanelHeight;
            if (index < Groups.Length - 1)
                height += PanelSpacing;
        }

        return height;
    }

    private StartRunLobby? GetCharacterSelectLobby()
    {
        var current = GetParent();
        while (current != null)
        {
            var lobbyProperty = current.GetType().GetProperty(
                "Lobby",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (lobbyProperty != null && typeof(StartRunLobby).IsAssignableFrom(lobbyProperty.PropertyType))
            {
                try
                {
                    if (lobbyProperty.GetValue(current) is StartRunLobby lobby)
                        return lobby;
                }
                catch
                {
                    // Ignore and keep walking.
                }
            }

            current = current.GetParent();
        }

        return null;
    }

    private LobbyGameplayNetRole GetCurrentRoleForUi()
    {
        return _lastObservedLobby != null
            ? LobbyGameplayNetRoleHelper.GetCurrentRole(_lastObservedLobby)
            : LobbyGameplayNetRoleHelper.GetCurrentRole(_lastObservedNetService);
    }

    private void RegisterHover(Control control, LucidDreamEntry entry)
    {
        var title = new LocString("settings_ui", entry.TitleKey);
        var description = GetText(entry.DescriptionKey);
        _hoverTipsByControl[control] =
        [
            new HoverTip(title, description)
            {
                Id = $"reastralparty.lucid_dream.{entry.Key}",
                IsInstanced = true
            }
        ];
        control.MouseEntered += () => ShowHoverTip(control);
        control.MouseExited += () => HideHoverTip(control);
    }

    private void ShowHoverTip(Control owner)
    {
        if (!_hoverTipsByControl.TryGetValue(owner, out var hoverTips))
            return;

        NHoverTipSet.CreateAndShow(owner, hoverTips, HoverTip.GetHoverTipAlignment(owner));
    }

    private static void HideHoverTip(Control owner)
    {
        NHoverTipSet.Remove(owner);
    }

    private void ClearAllHoverTips()
    {
        foreach (var owner in _hoverTipsByControl.Keys)
            NHoverTipSet.Remove(owner);
    }

    private static bool GetSettingValue(LobbyGameplaySettingsSnapshot snapshot, LucidDreamSettingKey key)
    {
        return key switch
        {
            LucidDreamSettingKey.FalseLifeline => snapshot.EnableLucidDreamFalseLifeline,
            LucidDreamSettingKey.SmoothSailing => snapshot.EnableLucidDreamSmoothSailing,
            LucidDreamSettingKey.FishScales => snapshot.EnableLucidDreamFishScalesMalice,
            LucidDreamSettingKey.SevereWoundOne => snapshot.EnableLucidDreamSevereWoundOneMalice,
            LucidDreamSettingKey.SevereWoundTwo => snapshot.EnableLucidDreamSevereWoundTwoMalice,
            LucidDreamSettingKey.MadLife => snapshot.EnableLucidDreamMadLifeMalice,
            LucidDreamSettingKey.SwampOfFate => snapshot.EnableLucidDreamSwampOfFateMalice,
            LucidDreamSettingKey.Overpopulation => snapshot.EnableLucidDreamOverpopulationMalice,
            LucidDreamSettingKey.CautiousJellyfish => snapshot.EnableLucidDreamCautiousJellyfishMalice,
            LucidDreamSettingKey.FaceDeathWithComposure => snapshot.EnableLucidDreamFaceDeathWithComposure,
            LucidDreamSettingKey.Wildness => snapshot.EnableLucidDreamWildness,
            LucidDreamSettingKey.PitchBlackImpulse => snapshot.EnableLucidDreamPitchBlackImpulse,
            LucidDreamSettingKey.BubblePotionOfDreams => snapshot.EnableLucidDreamBubblePotionOfDreams,
            LucidDreamSettingKey.HarmlessWhisper => snapshot.EnableLucidDreamHarmlessWhisper,
            _ => false
        };
    }

    private static void SetSettingValue(LobbyGameplaySettingsSnapshot snapshot, LucidDreamSettingKey key, bool value)
    {
        switch (key)
        {
            case LucidDreamSettingKey.FalseLifeline:
                snapshot.EnableLucidDreamFalseLifeline = value;
                break;
            case LucidDreamSettingKey.SmoothSailing:
                snapshot.EnableLucidDreamSmoothSailing = value;
                break;
            case LucidDreamSettingKey.FishScales:
                snapshot.EnableLucidDreamFishScalesMalice = value;
                break;
            case LucidDreamSettingKey.SevereWoundOne:
                snapshot.EnableLucidDreamSevereWoundOneMalice = value;
                break;
            case LucidDreamSettingKey.SevereWoundTwo:
                snapshot.EnableLucidDreamSevereWoundTwoMalice = value;
                break;
            case LucidDreamSettingKey.MadLife:
                snapshot.EnableLucidDreamMadLifeMalice = value;
                break;
            case LucidDreamSettingKey.SwampOfFate:
                snapshot.EnableLucidDreamSwampOfFateMalice = value;
                break;
            case LucidDreamSettingKey.Overpopulation:
                snapshot.EnableLucidDreamOverpopulationMalice = value;
                break;
            case LucidDreamSettingKey.CautiousJellyfish:
                snapshot.EnableLucidDreamCautiousJellyfishMalice = value;
                break;
            case LucidDreamSettingKey.FaceDeathWithComposure:
                snapshot.EnableLucidDreamFaceDeathWithComposure = value;
                break;
            case LucidDreamSettingKey.Wildness:
                snapshot.EnableLucidDreamWildness = value;
                break;
            case LucidDreamSettingKey.PitchBlackImpulse:
                snapshot.EnableLucidDreamPitchBlackImpulse = value;
                break;
            case LucidDreamSettingKey.BubblePotionOfDreams:
                snapshot.EnableLucidDreamBubblePotionOfDreams = value;
                break;
            case LucidDreamSettingKey.HarmlessWhisper:
                snapshot.EnableLucidDreamHarmlessWhisper = value;
                break;
        }
    }

    private static string GetText(string key)
    {
        return new LocString("settings_ui", key).GetRawText();
    }

    private static StyleBoxFlat CreateLucidDreamShellStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.09f, 0.12f, 0.95f),
            BorderColor = new Color(0.4f, 0.67f, 0.94f, 0.78f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = ShellMarginHorizontal,
            ContentMarginTop = ShellMarginVertical,
            ContentMarginRight = ShellMarginHorizontal,
            ContentMarginBottom = ShellMarginVertical
        };
    }

    private static StyleBoxFlat CreateIconButtonStyle(bool hover)
    {
        return new StyleBoxFlat
        {
            BgColor = hover
                ? new Color(0.18f, 0.22f, 0.27f, 0.48f)
                : new Color(0f, 0f, 0f, 0f),
            BorderColor = hover
                ? new Color(0.5f, 0.78f, 0.95f, 0.46f)
                : new Color(0f, 0f, 0f, 0f),
            BorderWidthLeft = hover ? 1 : 0,
            BorderWidthTop = hover ? 1 : 0,
            BorderWidthRight = hover ? 1 : 0,
            BorderWidthBottom = hover ? 1 : 0,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = IconButtonContentMargin,
            ContentMarginTop = IconButtonContentMargin,
            ContentMarginRight = IconButtonContentMargin,
            ContentMarginBottom = IconButtonContentMargin
        };
    }
}
