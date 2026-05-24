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
    }
}

internal static class CharacterSelectGameplayPreviewReadyPatch
{
    private const string PreviewNodeName = "ReAstralPartyMod_CharacterSelectGameplayPreview";

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

internal sealed partial class CharacterSelectGameplayPreviewPanel : Control
{
    private static readonly StartingPersonaMode[] StartingPersonaModes =
    [
        StartingPersonaMode.Standard,
        StartingPersonaMode.StandardDuplicate,
        StartingPersonaMode.RandomAssign,
        StartingPersonaMode.Clone,
        StartingPersonaMode.RandomClone
    ];

    private static readonly TokenSeriesMode[] TokenSeriesModes =
    [
        TokenSeriesMode.RandomTwo,
        TokenSeriesMode.All,
        TokenSeriesMode.Disabled
    ];

    private readonly Dictionary<Control, IReadOnlyList<IHoverTip>> _hoverTipsByControl = [];
    private CheckButton? _allPersonasToggle;
    private CheckButton? _allVariantPersonasToggle;
    private CheckButton? _extremeModeToggle;
    private OptionButton? _startingPersonaModeOption;
    private OptionButton? _tokenSeriesModeOption;
    private Label? _footerLabel;
    private Label? _stateLabel;
    private Godot.Timer? _refreshTimer;
    private bool _dragging;
    private Vector2 _dragPointerOffset;
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

    public CharacterSelectGameplayPreviewPanel()
    {
        AnchorLeft = 0f;
        AnchorTop = 0f;
        AnchorRight = 0f;
        AnchorBottom = 0f;
        OffsetLeft = 1140f;
        OffsetTop = 120f;
        OffsetRight = 1610f;
        OffsetBottom = 640f;
        MouseFilter = MouseFilterEnum.Pass;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _Ready()
    {
        BuildUi();
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
        ClearAllHoverTips();
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

        var body = new VBoxContainer
        {
            Name = "Body",
            MouseFilter = MouseFilterEnum.Pass,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 10);
        root.AddChild(body);

        body.AddChild(BuildIntroLabel());
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
            if (_allPersonasToggle != null)
            {
                _allPersonasToggle.ButtonPressed = snapshot.EnableAllPersonas;
                _allPersonasToggle.Disabled = !isEditable;
                _allPersonasToggle.Text = snapshot.EnableAllPersonas ? "ON" : "OFF";
            }

            if (_allVariantPersonasToggle != null)
            {
                _allVariantPersonasToggle.ButtonPressed = snapshot.EnableAllVariantPersonas;
                _allVariantPersonasToggle.Disabled = !isEditable;
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
                _startingPersonaModeOption.Disabled = !isEditable;
            }

            if (_tokenSeriesModeOption != null)
            {
                _tokenSeriesModeOption.Select(IndexOfTokenSeriesMode(snapshot.TokenSeriesMode));
                _tokenSeriesModeOption.Disabled = !isEditable;
            }

            if (_footerLabel != null)
            {
                _footerLabel.Text = role switch
                {
                    LobbyGameplayNetRole.Host or LobbyGameplayNetRole.Singleplayer => GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.footer_host"),
                    LobbyGameplayNetRole.Client => GetText("RE_ASTRAL_PARTY_MOD_SETTINGS.lobby_panel.footer_client"),
                    _ => "联机状态同步中，暂时只读。"
                };
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
        CallDeferred(nameof(ApplySnapshotDeferred), Variant.From(snapshot.EnableAllPersonas), Variant.From(snapshot.EnableAllVariantPersonas),
            Variant.From(snapshot.EnableExtremeMode), Variant.From((int)snapshot.StartingPersonaMode), Variant.From((int)snapshot.TokenSeriesMode));
    }

    private void ApplySnapshotDeferred(bool enableAllPersonas, bool enableAllVariantPersonas, bool enableExtremeMode,
        int startingPersonaMode, int tokenSeriesMode)
    {
        ApplySnapshotToUi(new LobbyGameplaySettingsSnapshot
        {
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

    private static string GetText(string key)
    {
        return new LocString("settings_ui", key).GetRawText();
    }

    private void OnRefreshTimerTimeout()
    {
        RefreshLobbySyncState();
    }

    private void OnTitleBarGuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }:
                _dragging = true;
                _dragPointerOffset = GetGlobalMousePosition() - GlobalPosition;
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }:
                _dragging = false;
                AcceptEvent();
                break;
            case InputEventMouseMotion when _dragging:
                var viewportRect = GetViewportRect();
                var panelSize = Size;
                var target = GetGlobalMousePosition() - _dragPointerOffset;
                target.X = Mathf.Clamp(target.X, 8f, Math.Max(8f, viewportRect.Size.X - panelSize.X - 8f));
                target.Y = Mathf.Clamp(target.Y, 8f, Math.Max(8f, viewportRect.Size.Y - panelSize.Y - 8f));
                GlobalPosition = target;
                AcceptEvent();
                break;
        }
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
