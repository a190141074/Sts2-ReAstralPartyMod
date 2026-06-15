using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using STS2RitsuLib.Patching.Builders;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class CharacterSelectGameplayPreviewPatchRegistrar
{
    public static void TryRegister(DynamicPatchBuilder builder)
    {
        var targetType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen");
        if (targetType == null)
            return;

        var readyMethod = AccessTools.DeclaredMethod(targetType, "_Ready", Type.EmptyTypes);
        if (readyMethod != null)
        {
            builder.Add(
                readyMethod,
                postfix: DynamicPatchBuilder.FromMethod(
                    typeof(CharacterSelectGameplayPreviewReadyPatch),
                    nameof(CharacterSelectGameplayPreviewReadyPatch.Postfix)),
                isCritical: false,
                description: "UI patch: inject a draggable lobby gameplay-settings window into character select",
                patchId: "character_select_gameplay_preview");
        }

        var exitTreeMethod = AccessTools.DeclaredMethod(targetType, "_ExitTree", Type.EmptyTypes);
        if (exitTreeMethod != null)
        {
            builder.Add(
                exitTreeMethod,
                prefix: DynamicPatchBuilder.FromMethod(
                    typeof(CharacterSelectGameplayPreviewExitPatch),
                    nameof(CharacterSelectGameplayPreviewExitPatch.Prefix)),
                isCritical: false,
                description: "UI patch: clear lobby gameplay-settings state when character select closes",
                patchId: "character_select_gameplay_preview_exit");
        }

        var loadGameScreenType = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NMultiplayerLoadGameScreen");
        if (loadGameScreenType != null)
        {
            var processMethod = AccessTools.DeclaredMethod(loadGameScreenType, "_Process", [typeof(double)]);
            if (processMethod != null)
            {
                builder.Add(
                    processMethod,
                    prefix: DynamicPatchBuilder.FromMethod(
                        typeof(CharacterSelectGameplayPreviewLoadGameRegistrationPatch),
                        nameof(CharacterSelectGameplayPreviewLoadGameRegistrationPatch.Prefix)),
                    isCritical: false,
                    description: "UI patch: register lobby gameplay-settings handlers before load-game screen inbound lobby packets are processed",
                    patchId: "character_select_gameplay_preview_load_game_register");
            }
        }
    }
}

internal static class CharacterSelectGameplayPreviewReadyPatch
{
    private const string PreviewNodeName = "ReAstralPartyMod_CharacterSelectGameplayPreview";
    private const string LucidDreamNodeName = "ReAstralPartyMod_CharacterSelectLucidDreamMalice";

    public static void Postfix(Control __instance)
    {
        if (__instance.GetNodeOrNull<Control>(PreviewNodeName) != null)
            return;

        LobbyGameplaySettingsSync.Register();

        var preview = new CharacterSelectGameplayPreviewPanel
        {
            Name = PreviewNodeName
        };
        __instance.AddChildSafely(preview);

        if (__instance.GetNodeOrNull<Control>(LucidDreamNodeName) == null)
        {
            var lucidDreamPanel = new CharacterSelectLucidDreamMalicePanel
            {
                Name = LucidDreamNodeName
            };
            __instance.AddChildSafely(lucidDreamPanel);
        }

        MainFile.Logger.Info(
            "Character select gameplay settings panel attached to MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect.NCharacterSelectScreen.");
    }
}

internal static class CharacterSelectGameplayPreviewExitPatch
{
    public static void Prefix()
    {
        LobbyGameplaySettingsSync.Unregister();
        LobbyGameplaySettingsSync.ClearIfLobbyClosedWithoutRunStart();
    }
}

internal static class CharacterSelectGameplayPreviewLoadGameRegistrationPatch
{
    public static void Prefix()
    {
        // Loaded-run join can receive the host's lobby snapshot before the character-select
        // preview timer notices NetService readiness, so register handlers on the screen's
        // frame loop as soon as a NetService exists.
        var netService = RunManager.Instance?.NetService;
        if (netService != null)
            LobbyGameplaySettingsSync.Register(netService);
    }
}

internal sealed partial class CharacterSelectGameplayPreviewPanel : Control
{
    private enum PanelInteractionMode
    {
        None = 0,
        Move = 1,
        ResizeLeft = 2,
        ResizeRight = 3,
        ResizeTop = 4,
        ResizeBottom = 5,
        ResizeTopLeft = 6,
        ResizeTopRight = 7,
        ResizeBottomLeft = 8,
        ResizeBottomRight = 9
    }

    private const float DefaultPanelPositionX = 1140f;
    private const float DefaultPanelPositionY = 120f;
    private const float DefaultPanelWidth = 440f;
    private const float DefaultPanelHeight = 520f;
    private const float PanelResizeHitThickness = 12f;
    private const float PanelViewportMargin = 8f;
    private const float PanelMinWidth = 360f;
    private const float PanelCollapsedHeight = 88f;
    private const float TitleBarHeight = 56f;
    private const float BodyMinVisibleHeight = 180f;
    private const float PanelAspectRatio = DefaultPanelWidth / DefaultPanelHeight;

    private static readonly StartingPersonaMode[] StartingPersonaModes =
    [
        StartingPersonaMode.Standard,
        StartingPersonaMode.StandardDuplicate,
        StartingPersonaMode.RandomAssign,
        StartingPersonaMode.Clone,
        StartingPersonaMode.RandomClone,
        StartingPersonaMode.DestinedClone
    ];

    private static readonly TokenSeriesMode[] TokenSeriesModes =
    [
        TokenSeriesMode.RandomTwo,
        TokenSeriesMode.All,
        TokenSeriesMode.Disabled
    ];

    private static readonly NeowExtraOptionSelectionMode[] NeowExtraOptionSelectionModes =
        Enum.GetValues<NeowExtraOptionSelectionMode>();

    private readonly Dictionary<Control, IReadOnlyList<IHoverTip>> _hoverTipsByControl = [];
    private CheckButton? _startingInitialPointToggle;
    private CheckButton? _startingRingOfSevenCursesToggle;
    private CheckButton? _startingPersonaSelectionToggle;
    private CheckButton? _dreamSeriesEventsToggle;
    private CheckButton? _enigmaticSeriesEventsToggle;
    private CheckButton? _neowExtraOptionToggle;
    private CheckButton? _allPersonasToggle;
    private CheckButton? _allVariantPersonasToggle;
    private CheckButton? _extremeModeToggle;
    private OptionButton? _startingPersonaModeOption;
    private OptionButton? _neowExtraOptionSelectionModeOption;
    private OptionButton? _tokenSeriesModeOption;
    private PanelContainer? _shell;
    private ScrollContainer? _bodyScroll;
    private VBoxContainer? _body;
    private Button? _collapseButton;
    private Label? _footerLabel;
    private Label? _stateLabel;
    private Godot.Timer? _refreshTimer;
    private Vector2 _displayedExpandedSize = new(DefaultPanelWidth, DefaultPanelHeight);
    private Vector2 _lastViewportSize = Vector2.Zero;
    private PanelInteractionMode _interactionMode;
    private Vector2 _interactionMouseStart;
    private Vector2 _interactionPositionStart;
    private Vector2 _interactionSizeStart = new(DefaultPanelWidth, DefaultPanelHeight);
    private Vector2 _expandedSize = new(DefaultPanelWidth, DefaultPanelHeight);
    private bool _isCollapsed;
    private bool _suppressUiEvents;
    private bool _handlersBound;
    private LobbyGameplayNetRole _lastKnownRole = LobbyGameplayNetRole.Pending;
    private INetGameService? _lastObservedNetService;
    private StartRunLobby? _lastObservedLobby;
    private double _syncPollAccumulator;
    private double _requestRetryAccumulator;
    private double _secondsSinceSnapshotUpdate;
    private bool _initialRoleSyncPerformed;
    private bool _syncErrorShown;
    private bool _hasReceivedSnapshotForCurrentSession;
    private readonly List<NeowExtraOptionSelectionMode> _visibleNeowExtraOptionSelectionModes = [];

    public CharacterSelectGameplayPreviewPanel()
    {
        AnchorLeft = 0f;
        AnchorTop = 0f;
        AnchorRight = 0f;
        AnchorBottom = 0f;
        OffsetLeft = DefaultPanelPositionX;
        OffsetTop = DefaultPanelPositionY;
        OffsetRight = DefaultPanelPositionX + DefaultPanelWidth;
        OffsetBottom = DefaultPanelPositionY + DefaultPanelHeight;
        MouseFilter = MouseFilterEnum.Pass;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        BuildUi();
        RestoreSavedPanelState();
        BuildRefreshTimer();
        LobbyGameplaySettingsSync.SnapshotChanged += OnSnapshotChanged;
        _handlersBound = true;
        RefreshLobbySyncState();
        RefreshFromCurrentState();
    }

    public override void _ExitTree()
    {
        if (_handlersBound)
            LobbyGameplaySettingsSync.SnapshotChanged -= OnSnapshotChanged;
        _handlersBound = false;
        EndInteraction(false);
        PersistPanelState();
        ClearAllHoverTips();
    }

    public override void _Process(double delta)
    {
        HandleViewportSizeChange();

        if (_interactionMode != PanelInteractionMode.None)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Left))
                UpdateInteraction(GetGlobalMousePosition());
            else
                EndInteraction();

            return;
        }

        UpdateCursorShape();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (_isCollapsed)
            return;

        HandleBodyInput(@event);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!IsVisibleInTree())
            return;

        if (@event is not InputEventKey keyEvent
            || !keyEvent.Pressed
            || keyEvent.Echo
            || keyEvent.Keycode != Key.Tab)
            return;

        ToggleCollapsed();
        GetViewport().SetInputAsHandled();
    }

    private void BuildUi()
    {
        var shadow = new ColorRect
        {
            Name = "Shadow",
            Color = new Color(0f, 0f, 0f, 0.34f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        shadow.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shadow.OffsetLeft = 10f;
        shadow.OffsetTop = 12f;
        shadow.OffsetRight = 10f;
        shadow.OffsetBottom = 12f;
        AddChild(shadow);

        var shell = new PanelContainer
        {
            Name = "Shell",
            MouseFilter = MouseFilterEnum.Pass
        };
        shell.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        shell.AddThemeStyleboxOverride("panel", CreateShellStyle());
        AddChild(shell);
        _shell = shell;

        var root = new VBoxContainer
        {
            Name = "Root",
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        root.AddThemeConstantOverride("separation", 12);
        shell.AddChild(root);

        root.AddChild(BuildTitleBar());

        var bodyScroll = new ScrollContainer
        {
            Name = "BodyScroll",
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        root.AddChild(bodyScroll);
        _bodyScroll = bodyScroll;

        var body = new VBoxContainer
        {
            Name = "Body",
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 10);
        bodyScroll.AddChild(body);
        _body = body;

        body.AddChild(BuildIntroLabel());
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_initial_point.description"),
            out _startingInitialPointToggle,
            OnEnableStartingInitialPointToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_ring_of_seven_curses.description"),
            out _startingRingOfSevenCursesToggle,
            OnEnableStartingRingOfSevenCursesToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_starting_persona_selection.description"),
            out _startingPersonaSelectionToggle,
            OnEnableStartingPersonaSelectionToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_dream_series_events.description"),
            out _dreamSeriesEventsToggle,
            OnEnableDreamSeriesEventsToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_enigmatic_series_events.description"),
            out _enigmaticSeriesEventsToggle,
            OnEnableEnigmaticSeriesEventsToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_neow_extra_option.description"),
            out _neowExtraOptionToggle,
            OnEnableNeowExtraOptionToggled));
        body.AddChild(BuildEnumRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.description"),
            NeowExtraOptionSelectionModes,
            ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionModeTitle,
            ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionModeDescription,
            out _neowExtraOptionSelectionModeOption,
            OnNeowExtraOptionSelectionModeSelected));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_personas.description"),
            out _allPersonasToggle,
            OnEnableAllPersonasToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_all_variant_personas.description"),
            out _allVariantPersonasToggle,
            OnEnableAllVariantPersonasToggled));
        body.AddChild(BuildBooleanRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.enable_extreme_mode.description"),
            out _extremeModeToggle,
            OnEnableExtremeModeToggled));
        body.AddChild(BuildEnumRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.description"),
            StartingPersonaModes,
            ReAstralPartyModSettingsManager.GetStartingPersonaModeTitle,
            ReAstralPartyModSettingsManager.GetStartingPersonaModeDescription,
            out _startingPersonaModeOption,
            OnStartingPersonaModeSelected));
        body.AddChild(BuildEnumRow(
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.label"),
            GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.token_series_mode.description"),
            TokenSeriesModes,
            ReAstralPartyModSettingsManager.GetTokenSeriesModeTitle,
            ReAstralPartyModSettingsManager.GetTokenSeriesModeDescription,
            out _tokenSeriesModeOption,
            OnTokenSeriesModeSelected));

        var spacer = new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        body.AddChild(spacer);

        _footerLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        _footerLabel.AddThemeColorOverride("font_color", new Color(0.92f, 0.91f, 0.84f, 0.82f));
        _footerLabel.AddThemeFontSizeOverride("font_size", 15);
        body.AddChild(_footerLabel);

        ApplyCollapsedState();
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

    private Control BuildTitleBar()
    {
        var titleBar = new PanelContainer
        {
            Name = "TitleBar",
            CustomMinimumSize = new Vector2(0f, 56f),
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.None
        };
        titleBar.AddThemeStyleboxOverride("panel", CreateTitleBarStyle());
        titleBar.GuiInput += OnTitleBarGuiInput;

        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 10);
        titleBar.AddChild(row);

        var badge = new ColorRect
        {
            Color = new Color(0.18f, 0.73f, 0.78f, 0.95f),
            CustomMinimumSize = new Vector2(8f, 28f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddChild(badge);

        var title = new Label
        {
            Text = "【星引擎·玩法设置】",
            MouseFilter = MouseFilterEnum.Ignore,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center
        };
        title.AddThemeColorOverride("font_color", new Color(0.98f, 0.9f, 0.42f));
        title.AddThemeFontSizeOverride("font_size", 24);
        row.AddChild(title);

        var grow = new Control
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore
        };
        row.AddChild(grow);

        _stateLabel = new Label
        {
            MouseFilter = MouseFilterEnum.Ignore
        };
        _stateLabel.AddThemeColorOverride("font_color", new Color(0.88f, 0.92f, 0.96f, 0.76f));
        _stateLabel.AddThemeFontSizeOverride("font_size", 16);
        row.AddChild(_stateLabel);

        _collapseButton = new Button
        {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.Click,
            CustomMinimumSize = new Vector2(68f, 34f),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            TooltipText = GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.collapse_tooltip")
        };
        _collapseButton.Pressed += ToggleCollapsed;
        row.AddChild(_collapseButton);
        RefreshCollapseButtonText();

        return titleBar;
    }

    private Control BuildIntroLabel()
    {
        var hint = new Label
        {
            Text = GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.gameplay.description"),
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        hint.AddThemeColorOverride("font_color", new Color(0.95f, 0.91f, 0.82f, 0.9f));
        hint.AddThemeFontSizeOverride("font_size", 17);
        return hint;
    }

    private Control BuildBooleanRow(
        string title,
        string description,
        out CheckButton toggle,
        Action<bool> handler)
    {
        var panel = CreateValueRowShell();
        var row = CreateValueRowContainer();
        panel.AddChild(row);

        row.AddChild(BuildRowText(title, description));

        var createdToggle = new CheckButton
        {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.Click,
            Text = string.Empty,
            CustomMinimumSize = new Vector2(86f, 36f),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd
        };
        createdToggle.Toggled += value =>
        {
            createdToggle.Text = value ? "ON" : "OFF";
            handler(value);
        };
        row.AddChild(createdToggle);
        RegisterHover(createdToggle, title, description);
        toggle = createdToggle;

        return panel;
    }

    private Control BuildEnumRow<TEnum>(
        string title,
        string description,
        IReadOnlyList<TEnum> values,
        Func<TEnum, string> titleResolver,
        Func<TEnum, string> descriptionResolver,
        out OptionButton optionButton,
        Action<long> handler) where TEnum : struct, Enum
    {
        var panel = CreateValueRowShell();
        var row = CreateValueRowContainer();
        panel.AddChild(row);

        row.AddChild(BuildRowText(title, description));

        optionButton = new OptionButton
        {
            MouseFilter = MouseFilterEnum.Stop,
            FocusMode = FocusModeEnum.Click,
            CustomMinimumSize = new Vector2(190f, 38f),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd
        };
        for (var i = 0; i < values.Count; i++)
            optionButton.AddItem(titleResolver(values[i]), i);

        optionButton.ItemSelected += selectedIndex => handler(selectedIndex);
        row.AddChild(optionButton);
        RegisterHover(optionButton, title, description);

        var popup = optionButton.GetPopup();
        for (var i = 0; i < values.Count; i++)
        {
            var optionTitle = titleResolver(values[i]);
            var optionDescription = descriptionResolver(values[i]);
            popup.SetItemTooltip(i, $"{optionTitle}\n{optionDescription}");
        }

        return panel;
    }

    private static PanelContainer CreateValueRowShell()
    {
        var panel = new PanelContainer
        {
            MouseFilter = MouseFilterEnum.Pass,
            CustomMinimumSize = new Vector2(0f, 72f)
        };
        panel.AddThemeStyleboxOverride("panel", CreateValueRowStyle());
        return panel;
    }

    private static HBoxContainer CreateValueRowContainer()
    {
        var row = new HBoxContainer
        {
            MouseFilter = MouseFilterEnum.Pass
        };
        row.AddThemeConstantOverride("separation", 10);
        return row;
    }

    private static Control BuildRowText(string title, string description)
    {
        var box = new VBoxContainer
        {
            MouseFilter = MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        box.AddThemeConstantOverride("separation", 4);

        var titleLabel = new Label
        {
            Text = title,
            MouseFilter = MouseFilterEnum.Ignore
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(0.96f, 0.95f, 0.91f));
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        box.AddChild(titleLabel);

        var descriptionLabel = new Label
        {
            Text = description,
            MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        descriptionLabel.AddThemeColorOverride("font_color", new Color(0.86f, 0.85f, 0.8f, 0.86f));
        descriptionLabel.AddThemeFontSizeOverride("font_size", 14);
        box.AddChild(descriptionLabel);
        return box;
    }

    private void RegisterHover(Control control, string title, string description)
    {
        _hoverTipsByControl[control] = [new HoverTip(new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.hover_title"), description)
        {
            Id = $"reastralparty.lobby_panel.{control.Name}.{title}",
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

    private void RefreshFromCurrentState()
    {
        var role = GetCurrentRoleForUi();
        var snapshot = LobbyGameplaySettingsSync.TryGetSnapshot(out var current)
            ? current
            : role == LobbyGameplayNetRole.Host
                ? LobbyGameplaySettingsSync.BuildFallbackSnapshot()
                : LobbyGameplaySettingsSync.BuildDefaultSnapshot();
        ApplySnapshotToUi(snapshot);
    }

    private void RefreshLobbySyncState()
    {
        _requestRetryAccumulator += 0.25d;
        _secondsSinceSnapshotUpdate += 0.25d;
        var lobby = GetCharacterSelectLobby();
        var netService = lobby?.NetService ?? RunManager.Instance?.NetService;
        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(lobby);
        var lobbyChanged = !ReferenceEquals(_lastObservedLobby, lobby);
        var netServiceChanged = !ReferenceEquals(_lastObservedNetService, netService);
        var roleChanged = role != _lastKnownRole;

        if (lobbyChanged)
        {
            _lastObservedLobby = lobby;
            _initialRoleSyncPerformed = false;
            _requestRetryAccumulator = 0d;
            _secondsSinceSnapshotUpdate = 0d;
            _hasReceivedSnapshotForCurrentSession = false;
            MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay preview observed lobby change; role={role} hostNetId={LobbyGameplayNetRoleHelper.GetHostNetId(lobby)} localNetId={lobby?.LocalPlayer.id ?? 0UL} players={lobby?.Players.Count ?? 0}.");
        }

        if (netServiceChanged)
        {
            LobbyGameplaySettingsSync.Register(netService);
            _lastObservedNetService = netService;
            _initialRoleSyncPerformed = false;
            _requestRetryAccumulator = 0d;
            _secondsSinceSnapshotUpdate = 0d;
            _hasReceivedSnapshotForCurrentSession = false;
            MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay preview observed NetService change; role={role}.");
        }

        if (roleChanged)
        {
            MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay preview role transition: {_lastKnownRole} -> {role}.");
            _lastKnownRole = role;
            _initialRoleSyncPerformed = false;
            _requestRetryAccumulator = 0d;
            _secondsSinceSnapshotUpdate = 0d;
            _syncErrorShown = false;
            _hasReceivedSnapshotForCurrentSession = false;
            RefreshFromCurrentState();
        }

        if (role == LobbyGameplayNetRole.Pending)
            return;

        if (role == LobbyGameplayNetRole.Singleplayer)
        {
            if (!_initialRoleSyncPerformed)
            {
                _initialRoleSyncPerformed = true;
                RefreshFromCurrentState();
            }

            return;
        }

        if (role == LobbyGameplayNetRole.Host)
        {
            if (!LobbyGameplaySettingsSync.TryGetSnapshot(out _))
            {
                LobbyGameplaySettingsSync.InitializeFromPersistent(ReAstralPartyModSettingsManager.ReadLocalSettings());
                MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay preview initialized authoritative host snapshot from persistent settings.");
            }

            _hasReceivedSnapshotForCurrentSession = true;

            if (!_initialRoleSyncPerformed)
            {
                _initialRoleSyncPerformed = true;
                LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
                _secondsSinceSnapshotUpdate = 0d;
            }

            return;
        }

        if (!_initialRoleSyncPerformed)
        {
            _initialRoleSyncPerformed = true;
            if (!_hasReceivedSnapshotForCurrentSession)
            {
                LobbyGameplaySettingsSync.RequestSnapshotFromHost();
                _requestRetryAccumulator = 0d;
            }
        }

        if (!_hasReceivedSnapshotForCurrentSession && _requestRetryAccumulator >= 1.5d)
        {
            _requestRetryAccumulator = 0d;
            LobbyGameplaySettingsSync.RequestSnapshotFromHost();
        }

        if (!_hasReceivedSnapshotForCurrentSession && !_syncErrorShown && _secondsSinceSnapshotUpdate >= 6d)
        {
            _syncErrorShown = true;
            AstralNotificationService.ShowDiagnosticError(
                AstralNotificationModule.Multiplayer,
                AstralNotificationArea.Multiplayer,
                219,
                "角色选择阶段在限定时间内未收到房主玩法设置快照，请检查主客机联机状态。",
                "房间玩法同步");
        }
    }

    private void ApplySnapshotToUi(LobbyGameplaySettingsSnapshot snapshot)
    {
        _suppressUiEvents = true;
        try
        {
            var role = GetCurrentRoleForUi();
            var isEditable = IsEditableByLocalPlayer(role);
            var personaSelectionEnabled = snapshot.EnableStartingPersonaSelection;
            var normalizedNeowSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                snapshot.EnableStartingRingOfSevenCurses,
                snapshot.NeowExtraOptionSelectionMode);
            if (_startingInitialPointToggle != null)
            {
                _startingInitialPointToggle.ButtonPressed = snapshot.EnableStartingInitialPoint;
                _startingInitialPointToggle.Disabled = !isEditable;
                _startingInitialPointToggle.Text = snapshot.EnableStartingInitialPoint ? "ON" : "OFF";
            }

            if (_startingRingOfSevenCursesToggle != null)
            {
                _startingRingOfSevenCursesToggle.ButtonPressed = snapshot.EnableStartingRingOfSevenCurses;
                _startingRingOfSevenCursesToggle.Disabled = !isEditable;
                _startingRingOfSevenCursesToggle.Text = snapshot.EnableStartingRingOfSevenCurses ? "ON" : "OFF";
            }

            if (_startingPersonaSelectionToggle != null)
            {
                _startingPersonaSelectionToggle.ButtonPressed = snapshot.EnableStartingPersonaSelection;
                _startingPersonaSelectionToggle.Disabled = !isEditable;
                _startingPersonaSelectionToggle.Text = snapshot.EnableStartingPersonaSelection ? "ON" : "OFF";
            }

            if (_dreamSeriesEventsToggle != null)
            {
                _dreamSeriesEventsToggle.ButtonPressed = snapshot.EnableDreamSeriesEvents;
                _dreamSeriesEventsToggle.Disabled = !isEditable;
                _dreamSeriesEventsToggle.Text = snapshot.EnableDreamSeriesEvents ? "ON" : "OFF";
            }

            if (_enigmaticSeriesEventsToggle != null)
            {
                _enigmaticSeriesEventsToggle.ButtonPressed = snapshot.EnableEnigmaticSeriesEvents;
                _enigmaticSeriesEventsToggle.Disabled = !isEditable;
                _enigmaticSeriesEventsToggle.Text = snapshot.EnableEnigmaticSeriesEvents ? "ON" : "OFF";
            }

            if (_neowExtraOptionToggle != null)
            {
                _neowExtraOptionToggle.ButtonPressed = snapshot.EnableNeowExtraOption;
                _neowExtraOptionToggle.Disabled = !isEditable;
                _neowExtraOptionToggle.Text = snapshot.EnableNeowExtraOption ? "ON" : "OFF";
            }

            if (_neowExtraOptionSelectionModeOption != null)
            {
                RebuildNeowExtraOptionSelectionModeItems(snapshot.EnableStartingRingOfSevenCurses);
                _neowExtraOptionSelectionModeOption.Select(
                    IndexOfVisibleNeowExtraOptionSelectionMode(normalizedNeowSelectionMode));
                _neowExtraOptionSelectionModeOption.Disabled = !isEditable || !snapshot.EnableNeowExtraOption;
            }

            if (_allPersonasToggle != null)
            {
                _allPersonasToggle.ButtonPressed = snapshot.EnableAllPersonas;
                _allPersonasToggle.Disabled = !isEditable || !personaSelectionEnabled;
                _allPersonasToggle.Text = snapshot.EnableAllPersonas ? "ON" : "OFF";
            }

            if (_allVariantPersonasToggle != null)
            {
                _allVariantPersonasToggle.ButtonPressed = snapshot.EnableAllVariantPersonas;
                _allVariantPersonasToggle.Disabled = !isEditable || !personaSelectionEnabled;
                _allVariantPersonasToggle.Text = snapshot.EnableAllVariantPersonas ? "ON" : "OFF";
            }

            if (_extremeModeToggle != null)
            {
                _extremeModeToggle.ButtonPressed = snapshot.EnableExtremeMode;
                _extremeModeToggle.Disabled = !isEditable;
                _extremeModeToggle.Text = snapshot.EnableExtremeMode ? "ON" : "OFF";
            }

            if (_startingPersonaModeOption != null)
            {
                _startingPersonaModeOption.Select(IndexOfStartingPersonaMode(snapshot.StartingPersonaMode));
                _startingPersonaModeOption.Disabled = !isEditable || !personaSelectionEnabled;
            }

            if (_tokenSeriesModeOption != null)
            {
                _tokenSeriesModeOption.Select(IndexOfTokenSeriesMode(snapshot.TokenSeriesMode));
                _tokenSeriesModeOption.Disabled = !isEditable;
            }

            if (_footerLabel != null)
            {
                var footer = role switch
                {
                    LobbyGameplayNetRole.Host or LobbyGameplayNetRole.Singleplayer => GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.footer_host"),
                    LobbyGameplayNetRole.Client => GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.footer_client"),
                    _ => "联机状态同步中，暂时只读。"
                };
                var lines = new List<string> { footer };
                if (!personaSelectionEnabled)
                    lines.Add(GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.starting_persona_mode.disabled_hint"));
                if (!snapshot.EnableNeowExtraOption)
                    lines.Add(GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.neow_extra_option_selection_mode.disabled_hint"));
                _footerLabel.Text = string.Join("\n", lines);
            }

            if (_stateLabel != null)
            {
                _stateLabel.Text = role switch
                {
                    LobbyGameplayNetRole.Host => "Host",
                    LobbyGameplayNetRole.Singleplayer => "Solo",
                    LobbyGameplayNetRole.Client => "Read Only",
                    _ => "Syncing"
                };
            }
        }
        finally
        {
            _suppressUiEvents = false;
        }
    }

    private static bool IsEditableByLocalPlayer(LobbyGameplayNetRole role)
    {
        return role == LobbyGameplayNetRole.Host;
    }

    private LobbyGameplayNetRole GetCurrentRoleForUi()
    {
        return _lastObservedLobby != null
            ? LobbyGameplayNetRoleHelper.GetCurrentRole(_lastObservedLobby)
            : LobbyGameplayNetRoleHelper.GetCurrentRole(_lastObservedNetService);
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
                    // Ignore and keep walking the parent chain.
                }
            }

            current = current.GetParent();
        }

        return null;
    }

    private void OnSnapshotChanged(LobbyGameplaySettingsSnapshot snapshot)
    {
        _secondsSinceSnapshotUpdate = 0d;
        _syncErrorShown = false;
        _hasReceivedSnapshotForCurrentSession = true;
        CallDeferred(nameof(ApplySnapshotDeferred), Variant.From(snapshot.EnableStartingInitialPoint),
            Variant.From(snapshot.EnableStartingRingOfSevenCurses),
            Variant.From(snapshot.EnableStartingPersonaSelection), Variant.From(snapshot.EnableDreamSeriesEvents),
            Variant.From(snapshot.EnableEnigmaticSeriesEvents), Variant.From(snapshot.EnableNeowExtraOption),
            Variant.From((int)snapshot.NeowExtraOptionSelectionMode),
            Variant.From(snapshot.EnableAllPersonas), Variant.From(snapshot.EnableAllVariantPersonas), Variant.From(snapshot.EnableExtremeMode),
            Variant.From((int)snapshot.StartingPersonaMode), Variant.From((int)snapshot.TokenSeriesMode));
    }

    private void ApplySnapshotDeferred(bool enableStartingInitialPoint, bool enableStartingRingOfSevenCurses, bool enableStartingPersonaSelection,
        bool enableDreamSeriesEvents, bool enableEnigmaticSeriesEvents, bool enableNeowExtraOption,
        int neowExtraOptionSelectionMode,
        bool enableAllPersonas, bool enableAllVariantPersonas, bool enableExtremeMode, int startingPersonaMode, int tokenSeriesMode)
    {
        ApplySnapshotToUi(new LobbyGameplaySettingsSnapshot
        {
            EnableStartingInitialPoint = enableStartingInitialPoint,
            EnableStartingRingOfSevenCurses = enableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = enableStartingPersonaSelection,
            EnableDreamSeriesEvents = enableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = enableEnigmaticSeriesEvents,
            EnableNeowExtraOption = enableNeowExtraOption,
            NeowExtraOptionSelectionMode = Enum.IsDefined(typeof(NeowExtraOptionSelectionMode), neowExtraOptionSelectionMode)
                ? (NeowExtraOptionSelectionMode)neowExtraOptionSelectionMode
                : NeowExtraOptionSelectionMode.DefaultRandom,
            EnableAllPersonas = enableAllPersonas,
            EnableAllVariantPersonas = enableAllVariantPersonas,
            EnableExtremeMode = enableExtremeMode,
            StartingPersonaMode = Enum.IsDefined(typeof(StartingPersonaMode), startingPersonaMode)
                ? (StartingPersonaMode)startingPersonaMode
                : StartingPersonaMode.Standard,
            TokenSeriesMode = Enum.IsDefined(typeof(TokenSeriesMode), tokenSeriesMode)
                ? (TokenSeriesMode)tokenSeriesMode
                : TokenSeriesMode.RandomTwo
        });
    }

    private void OnEnableAllPersonasToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableAllPersonas = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableStartingInitialPointToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableStartingInitialPoint = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableStartingRingOfSevenCursesToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableStartingRingOfSevenCurses = value);
        if (value)
        {
            LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot =>
            {
                if (snapshot.NeowExtraOptionSelectionMode == NeowExtraOptionSelectionMode.RingOfSevenCurses)
                    snapshot.NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode.DefaultRandom;
            });
        }
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableStartingPersonaSelectionToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableStartingPersonaSelection = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableDreamSeriesEventsToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableDreamSeriesEvents = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableEnigmaticSeriesEventsToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableEnigmaticSeriesEvents = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableNeowExtraOptionToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableNeowExtraOption = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnNeowExtraOptionSelectionModeSelected(long selectedIndex)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        if (selectedIndex < 0 || selectedIndex >= NeowExtraOptionSelectionModes.Length)
            return;

        if (selectedIndex >= _visibleNeowExtraOptionSelectionModes.Count)
            return;

        var selectedMode = _visibleNeowExtraOptionSelectionModes[(int)selectedIndex];
        selectedMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
            LobbyGameplaySettingsSync.TryGetSnapshot(out var currentSnapshot) && currentSnapshot.EnableStartingRingOfSevenCurses,
            selectedMode);
        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(
            snapshot => snapshot.NeowExtraOptionSelectionMode = selectedMode);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableAllVariantPersonasToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableAllVariantPersonas = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnEnableExtremeModeToggled(bool value)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.EnableExtremeMode = value);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnStartingPersonaModeSelected(long selectedIndex)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        if (selectedIndex < 0 || selectedIndex >= StartingPersonaModes.Length)
            return;

        var selectedMode = StartingPersonaModes[selectedIndex];
        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.StartingPersonaMode = selectedMode);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private void OnTokenSeriesModeSelected(long selectedIndex)
    {
        if (_suppressUiEvents || !IsEditableByLocalPlayer(GetCurrentRoleForUi()))
            return;

        if (selectedIndex < 0 || selectedIndex >= TokenSeriesModes.Length)
            return;

        var selectedMode = TokenSeriesModes[selectedIndex];
        LobbyGameplaySettingsSync.UpdateLocalLobbySnapshot(snapshot => snapshot.TokenSeriesMode = selectedMode);
        LobbyGameplaySettingsSync.BroadcastCurrentSnapshot();
    }

    private static int IndexOfStartingPersonaMode(StartingPersonaMode mode)
    {
        for (var i = 0; i < StartingPersonaModes.Length; i++)
        {
            if (StartingPersonaModes[i] == mode)
                return i;
        }

        return 0;
    }

    private static int IndexOfTokenSeriesMode(TokenSeriesMode mode)
    {
        for (var i = 0; i < TokenSeriesModes.Length; i++)
        {
            if (TokenSeriesModes[i] == mode)
                return i;
        }

        return 0;
    }

    private static int IndexOfNeowExtraOptionSelectionMode(NeowExtraOptionSelectionMode mode)
    {
        for (var i = 0; i < NeowExtraOptionSelectionModes.Length; i++)
        {
            if (NeowExtraOptionSelectionModes[i] == mode)
                return i;
        }

        return 0;
    }

    private void RebuildNeowExtraOptionSelectionModeItems(bool enableStartingRingOfSevenCurses)
    {
        if (_neowExtraOptionSelectionModeOption == null)
            return;

        _visibleNeowExtraOptionSelectionModes.Clear();
        _neowExtraOptionSelectionModeOption.Clear();

        foreach (var mode in NeowExtraOptionSelectionModes)
        {
            if (enableStartingRingOfSevenCurses && mode == NeowExtraOptionSelectionMode.RingOfSevenCurses)
                continue;

            _visibleNeowExtraOptionSelectionModes.Add(mode);
            _neowExtraOptionSelectionModeOption.AddItem(
                ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionModeTitle(mode),
                _visibleNeowExtraOptionSelectionModes.Count - 1);
        }

        var popup = _neowExtraOptionSelectionModeOption.GetPopup();
        for (var i = 0; i < _visibleNeowExtraOptionSelectionModes.Count; i++)
        {
            var mode = _visibleNeowExtraOptionSelectionModes[i];
            popup.SetItemTooltip(i,
                $"{ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionModeTitle(mode)}\n{ReAstralPartyModSettingsManager.GetNeowExtraOptionSelectionModeDescription(mode)}");
        }
    }

    private int IndexOfVisibleNeowExtraOptionSelectionMode(NeowExtraOptionSelectionMode mode)
    {
        for (var i = 0; i < _visibleNeowExtraOptionSelectionModes.Count; i++)
        {
            if (_visibleNeowExtraOptionSelectionModes[i] == mode)
                return i;
        }

        return 0;
    }

    private static string GetText(string key)
    {
        return new LocString("settings_ui", key).GetRawText();
    }

    private void OnRefreshTimerTimeout()
    {
        RefreshLobbySyncState();
    }

    private void HandleViewportSizeChange()
    {
        var viewportSize = GetViewportRect().Size;
        if (viewportSize.IsEqualApprox(_lastViewportSize))
            return;

        _lastViewportSize = viewportSize;
        RefreshDisplayedPanelRect();
    }

    private void HandleBodyInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } when TryBeginResize():
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _interactionMode != PanelInteractionMode.None:
                EndInteraction();
                AcceptEvent();
                break;
            case InputEventMouseMotion:
                UpdateCursorShape();
                break;
        }
    }

    private void OnTitleBarGuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                BeginMove();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _interactionMode == PanelInteractionMode.Move:
                EndInteraction();
                AcceptEvent();
                break;
        }
    }

    private void BeginMove()
    {
        _interactionMode = PanelInteractionMode.Move;
        _interactionMouseStart = GetGlobalMousePosition();
        _interactionPositionStart = GlobalPosition;
        _interactionSizeStart = Size;
    }

    private bool TryBeginResize()
    {
        var mode = ResolveResizeMode();
        if (mode == PanelInteractionMode.None)
            return false;

        _interactionMode = mode;
        _interactionMouseStart = GetGlobalMousePosition();
        _interactionPositionStart = GlobalPosition;
        _interactionSizeStart = Size;
        return true;
    }

    private void UpdateInteraction(Vector2 mousePosition)
    {
        switch (_interactionMode)
        {
            case PanelInteractionMode.Move:
                GlobalPosition = ClampPositionToViewport(_interactionPositionStart + (mousePosition - _interactionMouseStart), Size);
                break;
            case PanelInteractionMode.None:
                break;
            default:
                ApplyResize(mousePosition);
                break;
        }
    }

    private void ApplyResize(Vector2 mousePosition)
    {
        var startRect = new Rect2(_interactionPositionStart, _interactionSizeStart);
        var delta = mousePosition - _interactionMouseStart;
        var targetWidth = _interactionSizeStart.X;

        switch (_interactionMode)
        {
            case PanelInteractionMode.ResizeLeft:
                targetWidth = _interactionSizeStart.X - delta.X;
                break;
            case PanelInteractionMode.ResizeRight:
                targetWidth = _interactionSizeStart.X + delta.X;
                break;
            case PanelInteractionMode.ResizeTop:
                targetWidth = _interactionSizeStart.X - (delta.Y * PanelAspectRatio);
                break;
            case PanelInteractionMode.ResizeBottom:
                targetWidth = _interactionSizeStart.X + (delta.Y * PanelAspectRatio);
                break;
            case PanelInteractionMode.ResizeTopLeft:
            case PanelInteractionMode.ResizeBottomRight:
            case PanelInteractionMode.ResizeTopRight:
            case PanelInteractionMode.ResizeBottomLeft:
                var horizontal = _interactionMode is PanelInteractionMode.ResizeLeft or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeBottomLeft
                    ? -delta.X
                    : delta.X;
                var vertical = _interactionMode is PanelInteractionMode.ResizeTop or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeTopRight
                    ? -delta.Y * PanelAspectRatio
                    : delta.Y * PanelAspectRatio;
                targetWidth = _interactionSizeStart.X + (Mathf.Abs(horizontal) >= Mathf.Abs(vertical) ? horizontal : vertical);
                break;
        }

        var maxWidth = GetMaxExpandedWidthForCurrentViewport(_interactionMode);
        var clampedWidth = Mathf.Clamp(targetWidth, PanelMinWidth, Math.Max(PanelMinWidth, maxWidth));
        var targetSize = new Vector2(clampedWidth, clampedWidth / PanelAspectRatio);
        var targetPosition = startRect.Position;

        if (_interactionMode is PanelInteractionMode.ResizeLeft or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeBottomLeft)
            targetPosition.X = startRect.End.X - targetSize.X;

        if (_interactionMode is PanelInteractionMode.ResizeTop or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeTopRight)
            targetPosition.Y = startRect.End.Y - targetSize.Y;

        targetPosition = ClampPositionToViewport(targetPosition, targetSize);
        ApplyExpandedRect(targetPosition, targetSize);
    }

    private void EndInteraction(bool persist = true)
    {
        if (_interactionMode == PanelInteractionMode.None)
            return;

        _interactionMode = PanelInteractionMode.None;
        UpdateCursorShape();
        RefreshDisplayedPanelRect();
        if (persist)
            PersistPanelState();
    }

    private void ToggleCollapsed()
    {
        if (!_isCollapsed)
            _expandedSize = SanitizeExpandedSize(_displayedExpandedSize);

        _isCollapsed = !_isCollapsed;
        ApplyCollapsedState();
        PersistPanelState();
    }

    private void ApplyCollapsedState()
    {
        if (_bodyScroll != null)
            _bodyScroll.Visible = !_isCollapsed;

        if (_isCollapsed)
        {
            Size = new Vector2(_expandedSize.X, PanelCollapsedHeight);
            GlobalPosition = ClampPositionToViewport(GlobalPosition, Size);
            _displayedExpandedSize = BuildDisplayedExpandedSize(_expandedSize);
        }
        else
        {
            RefreshDisplayedPanelRect();
        }

        RefreshCollapseButtonText();
    }

    private void RefreshCollapseButtonText()
    {
        if (_collapseButton == null)
            return;

        _collapseButton.Text = _isCollapsed
            ? GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.expand_button")
            : GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.collapse_button");
        _collapseButton.TooltipText = _isCollapsed
            ? GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.expand_tooltip")
            : GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.collapse_tooltip");
    }

    private void RestoreSavedPanelState()
    {
        var state = ReAstralPartyModSettingsManager.LobbyPanelState;
        _isCollapsed = state.IsCollapsed;
        _expandedSize = SanitizeExpandedSize(new Vector2(state.Width, state.Height));
        _lastViewportSize = GetViewportRect().Size;
        var visibleSize = _isCollapsed
            ? new Vector2(_expandedSize.X, PanelCollapsedHeight)
            : BuildDisplayedExpandedSize(_expandedSize);
        _displayedExpandedSize = visibleSize;
        GlobalPosition = ClampPositionToViewport(new Vector2(state.PositionX, state.PositionY), visibleSize);
        Size = visibleSize;
        ApplyCollapsedState();
    }

    private void PersistPanelState()
    {
        var persistedSize = _expandedSize;
        ReAstralPartyModSettingsManager.UpdateLobbyPanelState(_isCollapsed, GlobalPosition, persistedSize);
    }

    private void ApplyExpandedRect(Vector2 position, Vector2 size)
    {
        _expandedSize = SanitizeExpandedSize(size);
        _displayedExpandedSize = BuildDisplayedExpandedSize(_expandedSize);
        Size = _displayedExpandedSize;
        GlobalPosition = ClampPositionToViewport(position, Size);
        RefreshBodyScrollLayout();
    }

    private Vector2 SanitizeExpandedSize(Vector2 size)
    {
        var minHeight = Math.Max(PanelMinWidth / PanelAspectRatio, PanelCollapsedHeight + BodyMinVisibleHeight);
        var maxWidth = GetMaxExpandedWidthForCurrentViewport(PanelInteractionMode.None);
        var width = Mathf.Clamp(size.X, PanelMinWidth, Math.Max(PanelMinWidth, maxWidth));
        var height = width / PanelAspectRatio;
        if (height < minHeight)
        {
            height = minHeight;
            width = height * PanelAspectRatio;
        }

        return new Vector2(width, height);
    }

    private void RefreshDisplayedPanelRect()
    {
        if (_isCollapsed)
        {
            Size = new Vector2(_expandedSize.X, PanelCollapsedHeight);
            GlobalPosition = ClampPositionToViewport(GlobalPosition, Size);
            return;
        }

        var displayedSize = BuildDisplayedExpandedSize(_expandedSize);
        _displayedExpandedSize = displayedSize;
        Size = displayedSize;
        GlobalPosition = ClampPositionToViewport(GlobalPosition, Size);
        RefreshBodyScrollLayout();
    }

    private Vector2 BuildDisplayedExpandedSize(Vector2 persistedExpandedSize)
    {
        var sanitizedExpanded = SanitizeExpandedSize(persistedExpandedSize);
        var viewportHeight = GetViewportRect().Size.Y;
        var maxVisibleHeight = Math.Max(
            PanelCollapsedHeight,
            viewportHeight - (PanelViewportMargin * 2f));
        var displayedHeight = Math.Min(sanitizedExpanded.Y, maxVisibleHeight);
        return new Vector2(sanitizedExpanded.X, displayedHeight);
    }

    private void RefreshBodyScrollLayout()
    {
        if (_bodyScroll == null)
            return;

        var shellContentHeight = Math.Max(0f, Size.Y - 32f);
        var bodyHeight = Math.Max(0f, shellContentHeight - TitleBarHeight - 12f);
        _bodyScroll.CustomMinimumSize = new Vector2(0f, Math.Max(0f, bodyHeight));
        _bodyScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
    }

    private float GetMaxExpandedWidthForCurrentViewport(PanelInteractionMode mode)
    {
        var viewport = GetViewportRect().Size;
        var availableWidth = Math.Max(PanelMinWidth, viewport.X - (PanelViewportMargin * 2f));
        var availableHeight = Math.Max(PanelCollapsedHeight, viewport.Y - (PanelViewportMargin * 2f));
        var widthFromHeight = availableHeight * PanelAspectRatio;
        var maxWidth = Math.Min(availableWidth, widthFromHeight);

        if (mode == PanelInteractionMode.None)
            return maxWidth;

        var anchor = _interactionPositionStart;
        var startRect = new Rect2(_interactionPositionStart, _interactionSizeStart);

        if (mode is PanelInteractionMode.ResizeLeft or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeBottomLeft)
            maxWidth = Math.Min(maxWidth, Math.Max(PanelMinWidth, startRect.End.X - PanelViewportMargin));

        if (mode is PanelInteractionMode.ResizeRight or PanelInteractionMode.ResizeTopRight or PanelInteractionMode.ResizeBottomRight)
            maxWidth = Math.Min(maxWidth, Math.Max(PanelMinWidth, viewport.X - anchor.X - PanelViewportMargin));

        if (mode is PanelInteractionMode.ResizeTop or PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeTopRight)
            maxWidth = Math.Min(maxWidth, Math.Max(PanelMinWidth, (startRect.End.Y - PanelViewportMargin) * PanelAspectRatio));

        if (mode is PanelInteractionMode.ResizeBottom or PanelInteractionMode.ResizeBottomLeft or PanelInteractionMode.ResizeBottomRight)
            maxWidth = Math.Min(maxWidth, Math.Max(PanelMinWidth, (viewport.Y - anchor.Y - PanelViewportMargin) * PanelAspectRatio));

        return Math.Max(PanelMinWidth, maxWidth);
    }

    private PanelInteractionMode ResolveResizeMode()
    {
        if (_isCollapsed)
            return PanelInteractionMode.None;

        var local = GetLocalMousePosition();
        var nearLeft = local.X <= PanelResizeHitThickness;
        var nearRight = local.X >= Size.X - PanelResizeHitThickness;
        var nearTop = local.Y <= PanelResizeHitThickness;
        var nearBottom = local.Y >= Size.Y - PanelResizeHitThickness;

        if (nearLeft && nearTop)
            return PanelInteractionMode.ResizeTopLeft;
        if (nearRight && nearTop)
            return PanelInteractionMode.ResizeTopRight;
        if (nearLeft && nearBottom)
            return PanelInteractionMode.ResizeBottomLeft;
        if (nearRight && nearBottom)
            return PanelInteractionMode.ResizeBottomRight;
        if (nearLeft)
            return PanelInteractionMode.ResizeLeft;
        if (nearRight)
            return PanelInteractionMode.ResizeRight;
        if (nearTop)
            return PanelInteractionMode.ResizeTop;
        if (nearBottom)
            return PanelInteractionMode.ResizeBottom;

        return PanelInteractionMode.None;
    }

    private void UpdateCursorShape()
    {
        MouseDefaultCursorShape = ResolveCursorShape(_interactionMode != PanelInteractionMode.None
            ? _interactionMode
            : ResolveResizeMode());
    }

    private static CursorShape ResolveCursorShape(PanelInteractionMode mode)
    {
        return mode switch
        {
            PanelInteractionMode.ResizeLeft or PanelInteractionMode.ResizeRight => CursorShape.Hsize,
            PanelInteractionMode.ResizeTop or PanelInteractionMode.ResizeBottom => CursorShape.Vsize,
            PanelInteractionMode.ResizeTopLeft or PanelInteractionMode.ResizeBottomRight => CursorShape.Fdiagsize,
            PanelInteractionMode.ResizeTopRight or PanelInteractionMode.ResizeBottomLeft => CursorShape.Bdiagsize,
            _ => CursorShape.Arrow
        };
    }

    private Vector2 ClampPositionToViewport(Vector2 target, Vector2 panelSize)
    {
        var viewportRect = GetViewportRect();
        target.X = Mathf.Clamp(target.X, PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportRect.Size.X - panelSize.X - PanelViewportMargin));
        target.Y = Mathf.Clamp(target.Y, PanelViewportMargin,
            Math.Max(PanelViewportMargin, viewportRect.Size.Y - panelSize.Y - PanelViewportMargin));
        return target;
    }

    private static StyleBoxFlat CreateShellStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.07f, 0.04f, 0.05f, 0.92f),
            BorderColor = new Color(0.81f, 0.57f, 0.23f, 0.88f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 12,
            CornerRadiusTopRight = 12,
            CornerRadiusBottomLeft = 12,
            CornerRadiusBottomRight = 12,
            ContentMarginLeft = 16,
            ContentMarginTop = 16,
            ContentMarginRight = 16,
            ContentMarginBottom = 16
        };
    }

    private static StyleBoxFlat CreateTitleBarStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.18f, 0.06f, 0.06f, 0.96f),
            BorderColor = new Color(0.98f, 0.72f, 0.23f, 0.9f),
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 14,
            ContentMarginTop = 10,
            ContentMarginRight = 14,
            ContentMarginBottom = 10
        };
    }

    private static StyleBoxFlat CreateValueRowStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.08f, 0.09f, 0.88f),
            BorderColor = new Color(0.48f, 0.26f, 0.18f, 0.82f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 12,
            ContentMarginTop = 10,
            ContentMarginRight = 12,
            ContentMarginBottom = 10
        };
    }
}
