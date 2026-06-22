using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Settings;

internal enum LobbyGameplayNetRole
{
    Pending = 0,
    Singleplayer = 1,
    Host = 2,
    Client = 3
}

internal static class LobbyGameplayNetRoleHelper
{
    public static LobbyGameplayNetRole GetCurrentRole(StartRunLobby? lobby)
    {
        if (lobby == null)
            return GetCurrentRole();

        var hostNetId = GetHostNetId(lobby);
        var localNetId = lobby.LocalPlayer.id;
        if (hostNetId != 0UL && localNetId != 0UL)
            return localNetId == hostNetId ? LobbyGameplayNetRole.Host : LobbyGameplayNetRole.Client;

        return GetCurrentRole(lobby.NetService);
    }

    public static LobbyGameplayNetRole GetCurrentRole(INetGameService? netService = null)
    {
        netService ??= RunManager.Instance?.NetService;
        if (netService == null)
            return LobbyGameplayNetRole.Pending;

        return netService.Type switch
        {
            NetGameType.Singleplayer => LobbyGameplayNetRole.Singleplayer,
            NetGameType.Host => LobbyGameplayNetRole.Host,
            NetGameType.Client => LobbyGameplayNetRole.Client,
            _ => LobbyGameplayNetRole.Pending
        };
    }

    public static ulong GetHostNetId(StartRunLobby? lobby)
    {
        if (lobby == null)
            return GetHostNetId();

        var players = lobby.Players;
        if (players is { Count: > 0 })
        {
            var hostPlayer = players
                .OrderBy(static player => player.slotId)
                .ThenBy(static player => player.id)
                .FirstOrDefault();
            if (hostPlayer.id != 0UL)
                return hostPlayer.id;
        }

        return GetHostNetId(lobby.NetService);
    }

    public static ulong GetHostNetId(INetGameService? netService = null)
    {
        netService ??= RunManager.Instance?.NetService;
        if (netService == null)
            return 0UL;

        var role = GetCurrentRole(netService);
        if (role == LobbyGameplayNetRole.Host)
            return netService.NetId;

        if (role != LobbyGameplayNetRole.Client || netService is not INetClientGameService clientService)
            return 0UL;

        return clientService.NetClient?.HostNetId ?? 0UL;
    }
}

public sealed class LobbyGameplaySettingsSnapshot
{
    public List<string> BannedRelicIdsSerialized { get; set; } = [];

    public AstralContentMode CurrentContentMode { get; set; } = AstralContentMode.Vanilla;

    public bool EnableStartingInitialPoint { get; set; }

    public bool EnableStartingAstralRelicStore { get; set; } = true;

    public bool EnableStartingRingOfSevenCurses { get; set; }

    public bool EnableStartingPersonSelection { get; set; } = true;

    public bool EnableDreamSeriesEvents { get; set; } = true;

    public bool EnableEnigmaticSeriesEvents { get; set; } = true;

    public bool EnableMoonPropShopSlots { get; set; } = true;

    public bool EnableMoonPropRelics { get; set; } = true;

    public bool EnableJewelryRelics { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;

    public bool EnableLucidDream { get; set; } = true;

    public bool EnableCollectorsCards { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersons { get; set; }

    public bool EnableVariantPersons { get; set; } = true;

    public bool EnableAllVariantPersons { get; set; }

    public bool EnableExtremeMode { get; set; }

    public StartingPersonMode StartingPersonMode { get; set; } = StartingPersonMode.Standard;

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;

    public bool EnableLucidDreamFishScalesMalice { get; set; }
    public bool EnableLucidDreamFalseLifeline { get; set; }
    public bool EnableLucidDreamSmoothSailing { get; set; }

    public bool EnableLucidDreamSevereWoundOneMalice { get; set; }

    public bool EnableLucidDreamSevereWoundTwoMalice { get; set; }

    public bool EnableLucidDreamMadLifeMalice { get; set; }

    public bool EnableLucidDreamSwampOfFateMalice { get; set; }

    public bool EnableLucidDreamOverpopulationMalice { get; set; }

    public bool EnableLucidDreamCautiousJellyfishMalice { get; set; }
    public bool EnableLucidDreamFaceDeathWithComposure { get; set; }
    public bool EnableLucidDreamWildness { get; set; }
    public bool EnableLucidDreamWildnessPhantom { get; set; }
    public bool EnableLucidDreamPitchBlackImpulse { get; set; }
    public bool EnableLucidDreamBubblePotionOfDreams { get; set; }
    public bool EnableLucidDreamHarmlessWhisper { get; set; }

    public bool EnableStartingPersonaSelection
    {
        get => EnableStartingPersonSelection;
        set => EnableStartingPersonSelection = value;
    }

    public bool EnableAllPersonas
    {
        get => EnableAllPersons;
        set => EnableAllPersons = value;
    }

    public bool EnableVariantPersonas
    {
        get => EnableVariantPersons;
        set => EnableVariantPersons = value;
    }

    public bool EnableAllVariantPersonas
    {
        get => EnableAllVariantPersons;
        set => EnableAllVariantPersons = value;
    }

    public StartingPersonMode StartingPersonaMode
    {
        get => StartingPersonMode;
        set => StartingPersonMode = value;
    }

    public LobbyGameplaySettingsSnapshot Clone()
    {
        return new LobbyGameplaySettingsSnapshot
        {
            CurrentContentMode = CurrentContentMode,
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = EnableMoonPropShopSlots,
            EnableMoonPropRelics = EnableMoonPropRelics,
            EnableJewelryRelics = EnableJewelryRelics,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableLucidDream = EnableLucidDream,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonMode = StartingPersonMode,
            TokenSeriesMode = TokenSeriesMode,
            EnableLucidDreamFishScalesMalice = EnableLucidDreamFishScalesMalice,
            EnableLucidDreamFalseLifeline = EnableLucidDreamFalseLifeline,
            EnableLucidDreamSmoothSailing = EnableLucidDreamSmoothSailing,
            EnableLucidDreamSevereWoundOneMalice = EnableLucidDreamSevereWoundOneMalice,
            EnableLucidDreamSevereWoundTwoMalice = EnableLucidDreamSevereWoundTwoMalice,
            EnableLucidDreamMadLifeMalice = EnableLucidDreamMadLifeMalice,
            EnableLucidDreamSwampOfFateMalice = EnableLucidDreamSwampOfFateMalice,
            EnableLucidDreamOverpopulationMalice = EnableLucidDreamOverpopulationMalice,
            EnableLucidDreamCautiousJellyfishMalice = EnableLucidDreamCautiousJellyfishMalice,
            EnableLucidDreamFaceDeathWithComposure = EnableLucidDreamFaceDeathWithComposure,
            EnableLucidDreamWildness = EnableLucidDreamWildness,
            EnableLucidDreamWildnessPhantom = EnableLucidDreamWildnessPhantom,
            EnableLucidDreamPitchBlackImpulse = EnableLucidDreamPitchBlackImpulse,
            EnableLucidDreamBubblePotionOfDreams = EnableLucidDreamBubblePotionOfDreams,
            EnableLucidDreamHarmlessWhisper = EnableLucidDreamHarmlessWhisper,
            BannedRelicIdsSerialized = [.. BannedRelicIdsSerialized]
        };
    }

    public static LobbyGameplaySettingsSnapshot FromPersistent(ReAstralPartyModSettings settings)
    {
        return new LobbyGameplaySettingsSnapshot
        {
            CurrentContentMode = ReAstralPartyModSettingsManager.GetCurrentContentMode(),
            EnableStartingInitialPoint = ReAstralPartyModSettingsManager.EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = ReAstralPartyModSettingsManager.EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = ReAstralPartyModSettingsManager.EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = ReAstralPartyModSettingsManager.EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = ReAstralPartyModSettingsManager.EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = ReAstralPartyModSettingsManager.EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = ReAstralPartyModSettingsManager.EnableMoonPropShopSlots,
            EnableMoonPropRelics = ReAstralPartyModSettingsManager.EnableMoonPropRelics,
            EnableJewelryRelics = ReAstralPartyModSettingsManager.EnableJewelryRelics,
            EnableNeowExtraOption = ReAstralPartyModSettingsManager.EnableNeowExtraOption,
            EnableLucidDream = ReAstralPartyModSettingsManager.EnableLucidDream,
            EnableCollectorsCards = ReAstralPartyModSettingsManager.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                ReAstralPartyModSettingsManager.EnableStartingRingOfSevenCurses,
                ReAstralPartyModSettingsManager.NeowExtraOptionSelectionMode),
            EnableAllPersonas = ReAstralPartyModSettingsManager.EnableAllPersonas,
            EnableVariantPersonas = ReAstralPartyModSettingsManager.EnableVariantPersonas,
            EnableAllVariantPersonas = ReAstralPartyModSettingsManager.EnableAllVariantPersonas,
            EnableExtremeMode = ReAstralPartyModSettingsManager.EnableExtremeMode,
            StartingPersonMode = ReAstralPartyModSettingsManager.ConfiguredStartingPersonaMode,
            TokenSeriesMode = ReAstralPartyModSettingsManager.TokenSeriesMode,
            BannedRelicIdsSerialized = [.. ReAstralPartyModSettingsManager.GetBannedRelicIds(null)
                .Where(static id => id != ModelId.none)
                .Select(static id => id.ToString())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static id => id, StringComparer.Ordinal)]
        };
    }
}

internal static class LobbyGameplaySettingsSync
{
    private static readonly object Gate = new();

    private static INetGameService? _registeredNetService;
    private static LobbyGameplaySettingsSnapshot? _currentSnapshot;
    private static bool _runStartPending;

    public static event Action<LobbyGameplaySettingsSnapshot>? SnapshotChanged;

    public static bool IsRunStartPending
    {
        get
        {
            lock (Gate)
            {
                return _runStartPending;
            }
        }
    }

    public static bool TryGetSnapshot(out LobbyGameplaySettingsSnapshot snapshot)
    {
        lock (Gate)
        {
            if (_currentSnapshot == null)
            {
                snapshot = null!;
                return false;
            }

            snapshot = _currentSnapshot.Clone();
            return true;
        }
    }

    public static void InitializeFromPersistent(ReAstralPartyModSettings settings)
    {
        SetSnapshotInternal(LobbyGameplaySettingsSnapshot.FromPersistent(settings), "initialize_from_persistent", false);
    }

    public static LobbyGameplaySettingsSnapshot BuildDefaultSnapshot()
    {
        return new LobbyGameplaySettingsSnapshot();
    }

    public static LobbyGameplaySettingsSnapshot BuildFallbackSnapshot()
    {
        return LobbyGameplaySettingsSnapshot.FromPersistent(ReAstralPartyModSettingsManager.ReadLocalSettings());
    }

    public static void ResetForContentModeSwitch()
    {
        SetSnapshotInternal(
            LobbyGameplaySettingsSnapshot.FromPersistent(ReAstralPartyModSettingsManager.ReadLocalSettings()),
            "content_mode_switch",
            true);
        BroadcastCurrentSnapshot();
    }

    public static void UpdateLocalLobbySnapshot(Action<LobbyGameplaySettingsSnapshot> mutator)
    {
        ArgumentNullException.ThrowIfNull(mutator);

        LobbyGameplaySettingsSnapshot updated;
        lock (Gate)
        {
            updated = (_currentSnapshot ?? BuildFallbackSnapshot()).Clone();
            mutator(updated);
            updated.NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                updated.EnableStartingRingOfSevenCurses,
                updated.NeowExtraOptionSelectionMode);
            _currentSnapshot = updated.Clone();
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot updated: content_mode={updated.CurrentContentMode}, start_initial_point={updated.EnableStartingInitialPoint}, start_astral_relic_store={updated.EnableStartingAstralRelicStore}, start_ring_of_seven_curses={updated.EnableStartingRingOfSevenCurses}, start_persona_selection={updated.EnableStartingPersonaSelection}, dream_series={updated.EnableDreamSeriesEvents}, enigmatic_series={updated.EnableEnigmaticSeriesEvents}, moon_shop_slots={updated.EnableMoonPropShopSlots}, moon_relics={updated.EnableMoonPropRelics}, neow_extra_option={updated.EnableNeowExtraOption}, lucid_dream={updated.EnableLucidDream}, neow_extra_selection={updated.NeowExtraOptionSelectionMode}, all_personas={updated.EnableAllPersonas}, variants_enabled={updated.EnableVariantPersonas}, all_variants={updated.EnableAllVariantPersonas}, extreme_mode={updated.EnableExtremeMode}, persona_mode={updated.StartingPersonMode}, token_series={updated.TokenSeriesMode}, banned_relics={updated.BannedRelicIdsSerialized.Count}");
        SnapshotChanged?.Invoke(updated.Clone());
    }

    public static void BroadcastCurrentSnapshot()
    {
        LobbyGameplaySettingsSnapshot? snapshot;
        INetGameService? netService;
        lock (Gate)
        {
            snapshot = _currentSnapshot?.Clone();
            netService = _registeredNetService;
        }

        if (snapshot == null || netService == null || LobbyGameplayNetRoleHelper.GetCurrentRole(netService) != LobbyGameplayNetRole.Host || !netService.IsConnected)
            return;

        netService.SendMessage(new AstralLobbyGameplaySettingsSnapshotMessage(snapshot));
        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot broadcast by host: content_mode={snapshot.CurrentContentMode}, start_initial_point={snapshot.EnableStartingInitialPoint}, start_astral_relic_store={snapshot.EnableStartingAstralRelicStore}, start_ring_of_seven_curses={snapshot.EnableStartingRingOfSevenCurses}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={snapshot.EnableMoonPropShopSlots}, moon_relics={snapshot.EnableMoonPropRelics}, neow_extra_option={snapshot.EnableNeowExtraOption}, lucid_dream={snapshot.EnableLucidDream}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, variants_enabled={snapshot.EnableVariantPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonMode}, token_series={snapshot.TokenSeriesMode}, banned_relics={snapshot.BannedRelicIdsSerialized.Count}");
    }

    public static void RequestSnapshotFromHost()
    {
        INetGameService? netService;
        lock (Gate)
        {
            netService = _registeredNetService;
        }

        if (netService == null || LobbyGameplayNetRoleHelper.GetCurrentRole(netService) != LobbyGameplayNetRole.Client || !netService.IsConnected)
            return;

        var hostNetId = LobbyGameplayNetRoleHelper.GetHostNetId(netService);
        if (hostNetId == 0UL)
            return;

        netService.SendMessage(new AstralLobbyGameplaySettingsRequestMessage(), hostNetId);
        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot requested from host {hostNetId}.");
    }

    public static void Register(INetGameService? netService = null)
    {
        netService ??= RunManager.Instance?.NetService;
        if (netService == null)
        {
            MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay settings message handler registration skipped because NetService was not ready.");
            return;
        }

        lock (Gate)
        {
            if (ReferenceEquals(_registeredNetService, netService))
                return;

            if (_registeredNetService != null)
                UnregisterHandlersLocked(_registeredNetService);

            netService.RegisterMessageHandler<AstralLobbyGameplaySettingsSnapshotMessage>(HandleSnapshotMessage);
            netService.RegisterMessageHandler<AstralLobbyGameplaySettingsRequestMessage>(HandleRequestMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay settings message handlers registered.");
    }

    public static void Unregister(INetGameService? netService = null)
    {
        lock (Gate)
        {
            netService ??= _registeredNetService ?? RunManager.Instance?.NetService;
            if (netService == null)
                return;

            UnregisterHandlersLocked(netService);
            if (ReferenceEquals(_registeredNetService, netService))
                _registeredNetService = null;
        }
    }

    public static void MarkRunStarting()
    {
        lock (Gate)
        {
            _runStartPending = true;
        }
    }

    public static void OnRunSnapshotEstablished()
    {
        lock (Gate)
        {
            _currentSnapshot = null;
            _runStartPending = false;
        }
    }

    public static void ClearIfLobbyClosedWithoutRunStart()
    {
        lock (Gate)
        {
            if (_runStartPending)
                return;

            _currentSnapshot = null;
        }
    }

    private static void HandleSnapshotMessage(AstralLobbyGameplaySettingsSnapshotMessage message, ulong senderId)
    {
        INetGameService? netService;
        lock (Gate)
        {
            netService = _registeredNetService;
        }

        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(netService);
        if (role == LobbyGameplayNetRole.Client)
        {
            var hostNetId = LobbyGameplayNetRoleHelper.GetHostNetId(netService);
            if (hostNetId == 0UL || senderId != hostNetId)
            {
                ReportLucidDreamSnapshotRejected(senderId, hostNetId, message);
                MainFile.Logger.Warn(
                    $"{MainFile.ModId} lobby gameplay snapshot rejected from non-host sender {senderId} (expected_host={hostNetId}).");
                return;
            }
        }

        var snapshot = message.ToSnapshot();
        SetSnapshotInternal(snapshot, $"remote_snapshot_from_{senderId}", true);
        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot received from {senderId}: content_mode={snapshot.CurrentContentMode}, start_initial_point={snapshot.EnableStartingInitialPoint}, start_astral_relic_store={snapshot.EnableStartingAstralRelicStore}, start_ring_of_seven_curses={snapshot.EnableStartingRingOfSevenCurses}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={snapshot.EnableMoonPropShopSlots}, moon_relics={snapshot.EnableMoonPropRelics}, neow_extra_option={snapshot.EnableNeowExtraOption}, lucid_dream={snapshot.EnableLucidDream}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, variants_enabled={snapshot.EnableVariantPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonMode}, token_series={snapshot.TokenSeriesMode}, banned_relics={snapshot.BannedRelicIdsSerialized.Count}");
    }

    private static void HandleRequestMessage(AstralLobbyGameplaySettingsRequestMessage message, ulong senderId)
    {
        INetGameService? netService;
        LobbyGameplaySettingsSnapshot? snapshot;
        lock (Gate)
        {
            netService = _registeredNetService;
            snapshot = _currentSnapshot?.Clone();
        }

        if (netService == null || LobbyGameplayNetRoleHelper.GetCurrentRole(netService) != LobbyGameplayNetRole.Host || !netService.IsConnected || snapshot == null)
            return;

        netService.SendMessage(new AstralLobbyGameplaySettingsSnapshotMessage(snapshot), senderId);
        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot resent to client {senderId}.");
    }

    private static void SetSnapshotInternal(LobbyGameplaySettingsSnapshot snapshot, string reason, bool invokeEvent)
    {
        lock (Gate)
        {
            _currentSnapshot = snapshot.Clone();
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot stored ({reason}): content_mode={snapshot.CurrentContentMode}, start_initial_point={snapshot.EnableStartingInitialPoint}, start_astral_relic_store={snapshot.EnableStartingAstralRelicStore}, start_ring_of_seven_curses={snapshot.EnableStartingRingOfSevenCurses}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, moon_shop_slots={snapshot.EnableMoonPropShopSlots}, moon_relics={snapshot.EnableMoonPropRelics}, neow_extra_option={snapshot.EnableNeowExtraOption}, lucid_dream={snapshot.EnableLucidDream}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, variants_enabled={snapshot.EnableVariantPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonMode}, token_series={snapshot.TokenSeriesMode}, banned_relics={snapshot.BannedRelicIdsSerialized.Count}");
        if (invokeEvent)
            SnapshotChanged?.Invoke(snapshot.Clone());
    }

    private static void UnregisterHandlersLocked(INetGameService netService)
    {
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsSnapshotMessage>(HandleSnapshotMessage);
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsRequestMessage>(HandleRequestMessage);
    }

    private static void ReportLucidDreamSnapshotRejected(
        ulong senderId,
        ulong expectedHostNetId,
        AstralLobbyGameplaySettingsSnapshotMessage message)
    {
        if (!HasAnyLucidDreamFlags(message))
            return;

        AstralNotificationService.ShowDiagnosticWarning(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.NeowDiagnostics,
            211,
            $"收到非房主来源的清醒梦房间设置同步，已拒绝应用。\n发送者：{senderId}\n预期房主：{expectedHostNetId}",
            "清醒梦同步异常");
    }

    private static bool HasAnyLucidDreamFlags(AstralLobbyGameplaySettingsSnapshotMessage message)
    {
        return message.EnableLucidDreamFishScalesMalice
               || message.EnableLucidDreamFalseLifeline
               || message.EnableLucidDreamSmoothSailing
               || message.EnableLucidDreamSevereWoundOneMalice
               || message.EnableLucidDreamSevereWoundTwoMalice
               || message.EnableLucidDreamMadLifeMalice
               || message.EnableLucidDreamSwampOfFateMalice
               || message.EnableLucidDreamOverpopulationMalice
               || message.EnableLucidDreamCautiousJellyfishMalice
               || message.EnableLucidDreamFaceDeathWithComposure
               || message.EnableLucidDreamWildness
               || message.EnableLucidDreamWildnessPhantom
               || message.EnableLucidDreamPitchBlackImpulse
               || message.EnableLucidDreamBubblePotionOfDreams
               || message.EnableLucidDreamHarmlessWhisper;
    }
}

public struct AstralLobbyGameplaySettingsSnapshotMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 18;

    public AstralContentMode CurrentContentMode { get; set; }
    public bool EnableStartingInitialPoint { get; set; }
    public bool EnableStartingAstralRelicStore { get; set; } = true;
    public bool EnableStartingRingOfSevenCurses { get; set; }
    public bool EnableStartingPersonaSelection { get; set; }
    public bool EnableDreamSeriesEvents { get; set; }
    public bool EnableEnigmaticSeriesEvents { get; set; }
    public bool EnableMoonPropShopSlots { get; set; } = true;
    public bool EnableMoonPropRelics { get; set; } = true;
    public bool EnableJewelryRelics { get; set; } = true;
    public bool EnableNeowExtraOption { get; set; }
    public bool EnableLucidDream { get; set; } = true;
    public bool EnableCollectorsCards { get; set; } = true;
    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; }
    public bool EnableAllPersonas { get; set; }
    public bool EnableVariantPersonas { get; set; } = true;
    public bool EnableAllVariantPersonas { get; set; }
    public bool EnableExtremeMode { get; set; }
    public StartingPersonMode StartingPersonMode { get; set; }
    public TokenSeriesMode TokenSeriesMode { get; set; }
    public bool EnableLucidDreamFishScalesMalice { get; set; }
    public bool EnableLucidDreamFalseLifeline { get; set; }
    public bool EnableLucidDreamSmoothSailing { get; set; }
    public bool EnableLucidDreamSevereWoundOneMalice { get; set; }
    public bool EnableLucidDreamSevereWoundTwoMalice { get; set; }
    public bool EnableLucidDreamMadLifeMalice { get; set; }
    public bool EnableLucidDreamSwampOfFateMalice { get; set; }
    public bool EnableLucidDreamOverpopulationMalice { get; set; }
    public bool EnableLucidDreamCautiousJellyfishMalice { get; set; }
    public bool EnableLucidDreamFaceDeathWithComposure { get; set; }
    public bool EnableLucidDreamWildness { get; set; }
    public bool EnableLucidDreamWildnessPhantom { get; set; }
    public bool EnableLucidDreamPitchBlackImpulse { get; set; }
    public bool EnableLucidDreamBubblePotionOfDreams { get; set; }
    public bool EnableLucidDreamHarmlessWhisper { get; set; }
    public List<string> BannedRelicIdsSerialized { get; set; }

    public AstralLobbyGameplaySettingsSnapshotMessage(LobbyGameplaySettingsSnapshot snapshot)
    {
        CurrentContentMode = snapshot.CurrentContentMode;
        EnableStartingInitialPoint = snapshot.EnableStartingInitialPoint;
        EnableStartingAstralRelicStore = snapshot.EnableStartingAstralRelicStore;
        EnableStartingRingOfSevenCurses = snapshot.EnableStartingRingOfSevenCurses;
        EnableStartingPersonaSelection = snapshot.EnableStartingPersonaSelection;
        EnableDreamSeriesEvents = snapshot.EnableDreamSeriesEvents;
        EnableEnigmaticSeriesEvents = snapshot.EnableEnigmaticSeriesEvents;
        EnableMoonPropShopSlots = snapshot.EnableMoonPropShopSlots;
        EnableMoonPropRelics = snapshot.EnableMoonPropRelics;
        EnableJewelryRelics = snapshot.EnableJewelryRelics;
        EnableNeowExtraOption = snapshot.EnableNeowExtraOption;
        EnableLucidDream = snapshot.EnableLucidDream;
        EnableCollectorsCards = snapshot.EnableCollectorsCards;
        NeowExtraOptionSelectionMode = snapshot.NeowExtraOptionSelectionMode;
        EnableAllPersonas = snapshot.EnableAllPersonas;
        EnableVariantPersonas = snapshot.EnableVariantPersonas;
        EnableAllVariantPersonas = snapshot.EnableAllVariantPersonas;
        EnableExtremeMode = snapshot.EnableExtremeMode;
        StartingPersonMode = snapshot.StartingPersonMode;
        TokenSeriesMode = snapshot.TokenSeriesMode;
        EnableLucidDreamFishScalesMalice = snapshot.EnableLucidDreamFishScalesMalice;
        EnableLucidDreamFalseLifeline = snapshot.EnableLucidDreamFalseLifeline;
        EnableLucidDreamSmoothSailing = snapshot.EnableLucidDreamSmoothSailing;
        EnableLucidDreamSevereWoundOneMalice = snapshot.EnableLucidDreamSevereWoundOneMalice;
        EnableLucidDreamSevereWoundTwoMalice = snapshot.EnableLucidDreamSevereWoundTwoMalice;
        EnableLucidDreamMadLifeMalice = snapshot.EnableLucidDreamMadLifeMalice;
        EnableLucidDreamSwampOfFateMalice = snapshot.EnableLucidDreamSwampOfFateMalice;
        EnableLucidDreamOverpopulationMalice = snapshot.EnableLucidDreamOverpopulationMalice;
        EnableLucidDreamCautiousJellyfishMalice = snapshot.EnableLucidDreamCautiousJellyfishMalice;
        EnableLucidDreamFaceDeathWithComposure = snapshot.EnableLucidDreamFaceDeathWithComposure;
        EnableLucidDreamWildness = snapshot.EnableLucidDreamWildness;
        EnableLucidDreamWildnessPhantom = snapshot.EnableLucidDreamWildnessPhantom;
        EnableLucidDreamPitchBlackImpulse = snapshot.EnableLucidDreamPitchBlackImpulse;
        EnableLucidDreamBubblePotionOfDreams = snapshot.EnableLucidDreamBubblePotionOfDreams;
        EnableLucidDreamHarmlessWhisper = snapshot.EnableLucidDreamHarmlessWhisper;
        BannedRelicIdsSerialized = [.. snapshot.BannedRelicIdsSerialized];
    }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteEnum(CurrentContentMode);
        writer.WriteBool(EnableStartingInitialPoint);
        writer.WriteBool(EnableStartingAstralRelicStore);
        writer.WriteBool(EnableStartingRingOfSevenCurses);
        writer.WriteBool(EnableStartingPersonaSelection);
        writer.WriteBool(EnableDreamSeriesEvents);
        writer.WriteBool(EnableEnigmaticSeriesEvents);
        writer.WriteBool(EnableMoonPropShopSlots);
        writer.WriteBool(EnableMoonPropRelics);
        writer.WriteBool(EnableJewelryRelics);
        writer.WriteBool(EnableNeowExtraOption);
        writer.WriteBool(EnableLucidDream);
        writer.WriteBool(EnableCollectorsCards);
        writer.WriteEnum(NeowExtraOptionSelectionMode);
        writer.WriteBool(EnableAllPersonas);
        writer.WriteBool(EnableVariantPersonas);
        writer.WriteBool(EnableAllVariantPersonas);
        writer.WriteBool(EnableExtremeMode);
        writer.WriteEnum(StartingPersonMode);
        writer.WriteEnum(TokenSeriesMode);
        writer.WriteBool(EnableLucidDreamFishScalesMalice);
        writer.WriteBool(EnableLucidDreamFalseLifeline);
        writer.WriteBool(EnableLucidDreamSmoothSailing);
        writer.WriteBool(EnableLucidDreamSevereWoundOneMalice);
        writer.WriteBool(EnableLucidDreamSevereWoundTwoMalice);
        writer.WriteBool(EnableLucidDreamMadLifeMalice);
        writer.WriteBool(EnableLucidDreamSwampOfFateMalice);
        writer.WriteBool(EnableLucidDreamOverpopulationMalice);
        writer.WriteBool(EnableLucidDreamCautiousJellyfishMalice);
        writer.WriteBool(EnableLucidDreamFaceDeathWithComposure);
        writer.WriteBool(EnableLucidDreamWildness);
        writer.WriteBool(EnableLucidDreamWildnessPhantom);
        writer.WriteBool(EnableLucidDreamPitchBlackImpulse);
        writer.WriteBool(EnableLucidDreamBubblePotionOfDreams);
        writer.WriteBool(EnableLucidDreamHarmlessWhisper);
        writer.WriteInt(BannedRelicIdsSerialized.Count);
        foreach (var bannedRelicId in BannedRelicIdsSerialized)
            writer.WriteString(bannedRelicId);
    }

    public void Deserialize(PacketReader reader)
    {
        var schemaVersion = reader.ReadInt();
        if (schemaVersion >= 14)
        {
            var rawMode = reader.ReadEnum<AstralContentMode>();
            CurrentContentMode = AstralContentModeRegistry.NormalizeMode(rawMode);
            EnableStartingInitialPoint = reader.ReadBool();
            EnableStartingAstralRelicStore = reader.ReadBool();
            EnableStartingRingOfSevenCurses = reader.ReadBool();
            EnableStartingPersonaSelection = reader.ReadBool();
        }
        else if (schemaVersion >= 10)
        {
            CurrentContentMode = AstralContentMode.Vanilla;
            EnableStartingInitialPoint = reader.ReadBool();
            EnableStartingAstralRelicStore = true;
            EnableStartingRingOfSevenCurses = reader.ReadBool();
            EnableStartingPersonaSelection = reader.ReadBool();
        }
        else
        {
            throw new InvalidOperationException($"{MainFile.ModId} received unsupported lobby gameplay snapshot schema {schemaVersion}.");
        }

        if (schemaVersion >= 18)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = reader.ReadBool();
            EnableMoonPropRelics = reader.ReadBool();
            EnableJewelryRelics = reader.ReadBool();
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = reader.ReadBool();
            EnableCollectorsCards = reader.ReadBool();
        }
        else if (schemaVersion >= 16)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = reader.ReadBool();
            EnableMoonPropRelics = reader.ReadBool();
            EnableJewelryRelics = true;
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = reader.ReadBool();
            EnableCollectorsCards = reader.ReadBool();
        }
        else if (schemaVersion >= 15)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = reader.ReadBool();
            EnableMoonPropRelics = true;
            EnableJewelryRelics = true;
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = reader.ReadBool();
            EnableCollectorsCards = reader.ReadBool();
        }
        else if (schemaVersion >= 12)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = reader.ReadBool();
            EnableMoonPropRelics = true;
            EnableJewelryRelics = true;
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = reader.ReadBool();
            EnableCollectorsCards = true;
        }
        else if (schemaVersion >= 11)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = reader.ReadBool();
            EnableMoonPropRelics = true;
            EnableJewelryRelics = true;
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = true;
            EnableCollectorsCards = true;
        }
        else if (schemaVersion >= 3)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableMoonPropShopSlots = true;
            EnableMoonPropRelics = true;
            EnableJewelryRelics = true;
            EnableNeowExtraOption = reader.ReadBool();
            EnableLucidDream = true;
            EnableCollectorsCards = true;
        }
        else
        {
            EnableDreamSeriesEvents = true;
            EnableEnigmaticSeriesEvents = true;
            EnableMoonPropShopSlots = true;
            EnableMoonPropRelics = true;
            EnableJewelryRelics = true;
            EnableNeowExtraOption = true;
            EnableLucidDream = true;
            EnableCollectorsCards = true;
        }

        if (schemaVersion >= 5)
        {
            var rawMode = reader.ReadEnum<NeowExtraOptionSelectionMode>();
            NeowExtraOptionSelectionMode = Enum.IsDefined(typeof(NeowExtraOptionSelectionMode), rawMode)
                ? rawMode
                : NeowExtraOptionSelectionMode.DefaultRandom;
        }
        else
        {
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode.DefaultRandom;
        }

        EnableAllPersonas = reader.ReadBool();
        if (schemaVersion >= 13)
        {
            EnableVariantPersonas = reader.ReadBool();
        }
        else
        {
            EnableVariantPersonas = true;
        }
        EnableAllVariantPersonas = reader.ReadBool();
        EnableExtremeMode = reader.ReadBool();
        StartingPersonMode = reader.ReadEnum<StartingPersonMode>();
        TokenSeriesMode = reader.ReadEnum<TokenSeriesMode>();

        if (schemaVersion >= 7)
        {
            EnableLucidDreamFishScalesMalice = reader.ReadBool();
            EnableLucidDreamFalseLifeline = reader.ReadBool();
            EnableLucidDreamSmoothSailing = reader.ReadBool();
            EnableLucidDreamSevereWoundOneMalice = reader.ReadBool();
            EnableLucidDreamSevereWoundTwoMalice = reader.ReadBool();
            EnableLucidDreamMadLifeMalice = reader.ReadBool();
            EnableLucidDreamSwampOfFateMalice = reader.ReadBool();
            EnableLucidDreamOverpopulationMalice = reader.ReadBool();
            EnableLucidDreamCautiousJellyfishMalice = reader.ReadBool();
            EnableLucidDreamFaceDeathWithComposure = reader.ReadBool();
            EnableLucidDreamWildness = reader.ReadBool();
            EnableLucidDreamWildnessPhantom = reader.ReadBool();
            EnableLucidDreamPitchBlackImpulse = reader.ReadBool();
            EnableLucidDreamBubblePotionOfDreams = reader.ReadBool();
            EnableLucidDreamHarmlessWhisper = reader.ReadBool();
        }
        else if (schemaVersion >= 6)
        {
            EnableLucidDreamFishScalesMalice = reader.ReadBool();
            EnableLucidDreamFalseLifeline = reader.ReadBool();
            EnableLucidDreamSmoothSailing = reader.ReadBool();
            EnableLucidDreamSevereWoundOneMalice = reader.ReadBool();
            EnableLucidDreamSevereWoundTwoMalice = reader.ReadBool();
            EnableLucidDreamMadLifeMalice = reader.ReadBool();
            EnableLucidDreamSwampOfFateMalice = reader.ReadBool();
            EnableLucidDreamOverpopulationMalice = reader.ReadBool();
            EnableLucidDreamCautiousJellyfishMalice = reader.ReadBool();
            EnableLucidDreamFaceDeathWithComposure = reader.ReadBool();
            EnableLucidDreamWildness = reader.ReadBool();
            EnableLucidDreamWildnessPhantom = false;
            EnableLucidDreamPitchBlackImpulse = reader.ReadBool();
            EnableLucidDreamBubblePotionOfDreams = reader.ReadBool();
            EnableLucidDreamHarmlessWhisper = reader.ReadBool();
        }
        else if (schemaVersion >= 4)
        {
            EnableLucidDreamFishScalesMalice = reader.ReadBool();
            EnableLucidDreamFalseLifeline = false;
            EnableLucidDreamSmoothSailing = false;
            EnableLucidDreamSevereWoundOneMalice = reader.ReadBool();
            EnableLucidDreamSevereWoundTwoMalice = reader.ReadBool();
            EnableLucidDreamMadLifeMalice = reader.ReadBool();
            EnableLucidDreamSwampOfFateMalice = reader.ReadBool();
            EnableLucidDreamOverpopulationMalice = reader.ReadBool();
            EnableLucidDreamCautiousJellyfishMalice = reader.ReadBool();
            EnableLucidDreamFaceDeathWithComposure = false;
            EnableLucidDreamWildness = false;
            EnableLucidDreamWildnessPhantom = false;
            EnableLucidDreamPitchBlackImpulse = false;
            EnableLucidDreamBubblePotionOfDreams = false;
            EnableLucidDreamHarmlessWhisper = false;
        }
        else
        {
            EnableLucidDreamFishScalesMalice = false;
            EnableLucidDreamFalseLifeline = false;
            EnableLucidDreamSmoothSailing = false;
            EnableLucidDreamSevereWoundOneMalice = false;
            EnableLucidDreamSevereWoundTwoMalice = false;
            EnableLucidDreamMadLifeMalice = false;
            EnableLucidDreamSwampOfFateMalice = false;
            EnableLucidDreamOverpopulationMalice = false;
            EnableLucidDreamCautiousJellyfishMalice = false;
            EnableLucidDreamFaceDeathWithComposure = false;
            EnableLucidDreamWildness = false;
            EnableLucidDreamWildnessPhantom = false;
            EnableLucidDreamPitchBlackImpulse = false;
            EnableLucidDreamBubblePotionOfDreams = false;
            EnableLucidDreamHarmlessWhisper = false;
        }

        if (schemaVersion >= 17)
        {
            var count = reader.ReadInt();
            var bannedRelicIds = new List<string>(count);
            for (var index = 0; index < count; index++)
                bannedRelicIds.Add(reader.ReadString());
            BannedRelicIdsSerialized = bannedRelicIds;
        }
        else
        {
            BannedRelicIdsSerialized = [];
        }
    }

    public LobbyGameplaySettingsSnapshot ToSnapshot()
    {
        return new LobbyGameplaySettingsSnapshot
        {
            CurrentContentMode = CurrentContentMode,
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
            EnableStartingRingOfSevenCurses = EnableStartingRingOfSevenCurses,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableDreamSeriesEvents = EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
            EnableMoonPropShopSlots = EnableMoonPropShopSlots,
            EnableMoonPropRelics = EnableMoonPropRelics,
            EnableJewelryRelics = EnableJewelryRelics,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableLucidDream = EnableLucidDream,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonMode = StartingPersonMode,
            TokenSeriesMode = TokenSeriesMode,
            EnableLucidDreamFishScalesMalice = EnableLucidDreamFishScalesMalice,
            EnableLucidDreamFalseLifeline = EnableLucidDreamFalseLifeline,
            EnableLucidDreamSmoothSailing = EnableLucidDreamSmoothSailing,
            EnableLucidDreamSevereWoundOneMalice = EnableLucidDreamSevereWoundOneMalice,
            EnableLucidDreamSevereWoundTwoMalice = EnableLucidDreamSevereWoundTwoMalice,
            EnableLucidDreamMadLifeMalice = EnableLucidDreamMadLifeMalice,
            EnableLucidDreamSwampOfFateMalice = EnableLucidDreamSwampOfFateMalice,
            EnableLucidDreamOverpopulationMalice = EnableLucidDreamOverpopulationMalice,
            EnableLucidDreamCautiousJellyfishMalice = EnableLucidDreamCautiousJellyfishMalice,
            EnableLucidDreamFaceDeathWithComposure = EnableLucidDreamFaceDeathWithComposure,
            EnableLucidDreamWildness = EnableLucidDreamWildness,
            EnableLucidDreamWildnessPhantom = EnableLucidDreamWildnessPhantom,
            EnableLucidDreamPitchBlackImpulse = EnableLucidDreamPitchBlackImpulse,
            EnableLucidDreamBubblePotionOfDreams = EnableLucidDreamBubblePotionOfDreams,
            EnableLucidDreamHarmlessWhisper = EnableLucidDreamHarmlessWhisper,
            BannedRelicIdsSerialized = [.. BannedRelicIdsSerialized]
        };
    }
}

public struct AstralLobbyGameplaySettingsRequestMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 1;

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
    }

    public void Deserialize(PacketReader reader)
    {
        _ = reader.ReadInt();
    }
}
