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
    private enum LucidDreamMaliceKey
    {
        FishScales = 0,
        SevereWoundOne = 1,
        SevereWoundTwo = 2,
        MadLife = 3,
        SwampOfFate = 4,
        Overpopulation = 5,
        CautiousJellyfish = 6
    }

    private sealed record LucidDreamMaliceEntry(
        LucidDreamMaliceKey Key,
        string TexturePath,
        string TitleKey,
        string DescriptionKey);

    private sealed class LucidDreamToggleView
    {
        public required Button Button { get; init; }

        public required Label CheckLabel { get; init; }

        public required LucidDreamMaliceEntry Entry { get; init; }
    }

    private const float PanelWidth = 470f;
    private const float PanelHeight = 170f;
    private const float PanelViewportMargin = 36f;

    private static readonly LucidDreamMaliceEntry[] Entries =
    [
        new(
            LucidDreamMaliceKey.FishScales,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_fish_scales.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.fish_scales.description"),
        new(
            LucidDreamMaliceKey.SevereWoundOne,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_severe_wound_one.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_one.description"),
        new(
            LucidDreamMaliceKey.SevereWoundTwo,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_severe_wound_two.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.severe_wound_two.description"),
        new(
            LucidDreamMaliceKey.MadLife,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_mad_life.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.mad_life.description"),
        new(
            LucidDreamMaliceKey.SwampOfFate,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_swamp_of_fate.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.swamp_of_fate.description"),
        new(
            LucidDreamMaliceKey.Overpopulation,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_overpopulation.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.overpopulation.description"),
        new(
            LucidDreamMaliceKey.CautiousJellyfish,
            "res://ReAstralPartyMod/images/ui/dream_lucid/dream_lucid_cautious_jellyfish.jpg",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.title",
            "RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.cautious_jellyfish.description")
    ];

    private readonly Dictionary<Control, IReadOnlyList<IHoverTip>> _hoverTipsByControl = [];
    private readonly List<LucidDreamToggleView> _toggleViews = [];
    private Godot.Timer? _refreshTimer;
    private Vector2 _lastViewportSize = Vector2.Zero;
    private bool _handlersBound;
    private LobbyGameplayNetRole _lastKnownRole = LobbyGameplayNetRole.Pending;
    private INetGameService? _lastObservedNetService;
    private StartRunLobby? _lastObservedLobby;

    public CharacterSelectLucidDreamMalicePanel()
    {
        CustomMinimumSize = new Vector2(PanelWidth, PanelHeight);
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
        var shadow = new ColorRect
        {
            Color = new Color(0f, 0f, 0f, 0.3f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        shadow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shadow.OffsetLeft = 8f;
        shadow.OffsetTop = 10f;
        shadow.OffsetRight = 8f;
        shadow.OffsetBottom = 10f;
        AddChild(shadow);

        var shell = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        shell.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shell.AddThemeStyleboxOverride("panel", CreateLucidDreamShellStyle());
        AddChild(shell);

        var root = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        root.AddThemeConstantOverride("separation", 8);
        shell.AddChild(root);

        var title = new Label
        {
            Text = GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lucid_dream_malice.title"),
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        title.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 0.96f));
        title.AddThemeFontSizeOverride("font_size", 18);
        root.AddChild(title);

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        row.AddThemeConstantOverride("separation", 10);
        root.AddChild(row);

        for (var index = 0; index < Entries.Length; index++)
            row.AddChild(BuildIconToggle(Entries[index], index));
    }

    private Control BuildIconToggle(LucidDreamMaliceEntry entry, int index)
    {
        var button = new Button
        {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None,
            Flat = true,
            CustomMinimumSize = new Vector2(60f, 82f),
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
        content.AddThemeConstantOverride("separation", 2);
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
            CustomMinimumSize = new Vector2(51f, 51f),
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
        button.Name = $"LucidDreamIcon{index}";
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
            var isEnabled = GetMaliceValue(snapshot, toggleView.Entry.Key);
            toggleView.CheckLabel.Visible = isEnabled;
            toggleView.Button.Modulate = isEditable
                ? Colors.White
                : new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private void OnSnapshotChanged(LobbyGameplaySettingsSnapshot snapshot)
    {
        CallDeferred(nameof(ApplySnapshotDeferred),
            Variant.From(snapshot.EnableLucidDreamFishScalesMalice),
            Variant.From(snapshot.EnableLucidDreamSevereWoundOneMalice),
            Variant.From(snapshot.EnableLucidDreamSevereWoundTwoMalice),
            Variant.From(snapshot.EnableLucidDreamMadLifeMalice),
            Variant.From(snapshot.EnableLucidDreamSwampOfFateMalice),
            Variant.From(snapshot.EnableLucidDreamOverpopulationMalice),
            Variant.From(snapshot.EnableLucidDreamCautiousJellyfishMalice));
    }

    private void ApplySnapshotDeferred(
        bool fishScales,
        bool severeWoundOne,
        bool severeWoundTwo,
        bool madLife,
        bool swampOfFate,
        bool overpopulation,
        bool cautiousJellyfish)
    {
        ApplySnapshot(new LobbyGameplaySettingsSnapshot
        {
            EnableLucidDreamFishScalesMalice = fishScales,
            EnableLucidDreamSevereWoundOneMalice = severeWoundOne,
            EnableLucidDreamSevereWoundTwoMalice = severeWoundTwo,
            EnableLucidDreamMadLifeMalice = madLife,
            EnableLucidDreamSwampOfFateMalice = swampOfFate,
            EnableLucidDreamOverpopulationMalice = overpopulation,
            EnableLucidDreamCautiousJellyfishMalice = cautiousJellyfish
        });
    }

    private void OnTogglePressed(LucidDreamMaliceKey key)
    {
        if (GetCurrentRoleForUi() != LobbyGameplayNetRole.Host)
            return;

        var currentSnapshot = LobbyGameplaySettingsSync.TryGetSnapshot(out var snapshot)
            ? snapshot
            : LobbyGameplaySettingsSync.BuildFallbackSnapshot();
        var nextValue = !GetMaliceValue(currentSnapshot, key);
        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshotToMutate => SetMaliceValue(snapshotToMutate, key, nextValue));
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnRefreshTimerTimeout()
    {
        RefreshRoleState();
    }

    private void PositionPanel()
    {
        var viewportSize = GetViewportRect().Size;
        var x = Mathf.Clamp(
            viewportSize.X - PanelWidth - 96f,
            PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportSize.X - PanelWidth - PanelViewportMargin));
        var y = Mathf.Clamp(
            (viewportSize.Y - PanelHeight) * 0.5f,
            PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportSize.Y - PanelHeight - PanelViewportMargin));
        GlobalPosition = new Vector2(x, y);
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

    private void RegisterHover(Control control, LucidDreamMaliceEntry entry)
    {
        var title = new LocString("settings_ui", entry.TitleKey);
        var description = GetText(entry.DescriptionKey);
        _hoverTipsByControl[control] = [new HoverTip(title, description)
        {
            Id = $"reastralparty.lucid_dream_malice.{entry.Key}",
            IsInstanced = true
        }];
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

    private static bool GetMaliceValue(LobbyGameplaySettingsSnapshot snapshot, LucidDreamMaliceKey key)
    {
        return key switch
        {
            LucidDreamMaliceKey.FishScales => snapshot.EnableLucidDreamFishScalesMalice,
            LucidDreamMaliceKey.SevereWoundOne => snapshot.EnableLucidDreamSevereWoundOneMalice,
            LucidDreamMaliceKey.SevereWoundTwo => snapshot.EnableLucidDreamSevereWoundTwoMalice,
            LucidDreamMaliceKey.MadLife => snapshot.EnableLucidDreamMadLifeMalice,
            LucidDreamMaliceKey.SwampOfFate => snapshot.EnableLucidDreamSwampOfFateMalice,
            LucidDreamMaliceKey.Overpopulation => snapshot.EnableLucidDreamOverpopulationMalice,
            LucidDreamMaliceKey.CautiousJellyfish => snapshot.EnableLucidDreamCautiousJellyfishMalice,
            _ => false
        };
    }

    private static void SetMaliceValue(LobbyGameplaySettingsSnapshot snapshot, LucidDreamMaliceKey key, bool value)
    {
        switch (key)
        {
            case LucidDreamMaliceKey.FishScales:
                snapshot.EnableLucidDreamFishScalesMalice = value;
                break;
            case LucidDreamMaliceKey.SevereWoundOne:
                snapshot.EnableLucidDreamSevereWoundOneMalice = value;
                break;
            case LucidDreamMaliceKey.SevereWoundTwo:
                snapshot.EnableLucidDreamSevereWoundTwoMalice = value;
                break;
            case LucidDreamMaliceKey.MadLife:
                snapshot.EnableLucidDreamMadLifeMalice = value;
                break;
            case LucidDreamMaliceKey.SwampOfFate:
                snapshot.EnableLucidDreamSwampOfFateMalice = value;
                break;
            case LucidDreamMaliceKey.Overpopulation:
                snapshot.EnableLucidDreamOverpopulationMalice = value;
                break;
            case LucidDreamMaliceKey.CautiousJellyfish:
                snapshot.EnableLucidDreamCautiousJellyfishMalice = value;
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
            ContentMarginLeft = 18,
            ContentMarginTop = 10,
            ContentMarginRight = 18,
            ContentMarginBottom = 10
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
            ContentMarginLeft = 2,
            ContentMarginTop = 2,
            ContentMarginRight = 2,
            ContentMarginBottom = 2
        };
    }
}
