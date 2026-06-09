using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;

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
    public bool EnableStartingInitialPoint { get; set; }

    public bool EnableStartingPersonaSelection { get; set; } = true;

    public bool EnableDreamMode { get; set; }

    public bool EnableDreamSeriesEvents { get; set; } = true;

    public bool EnableEnigmaticSeriesEvents { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersonas { get; set; }

    public bool EnableAllVariantPersonas { get; set; }

    public bool EnableExtremeMode { get; set; }

    public StartingPersonaMode StartingPersonaMode { get; set; } = StartingPersonaMode.Standard;

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

    public LobbyGameplaySettingsSnapshot Clone()
    {
        return new LobbyGameplaySettingsSnapshot
        {
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableDreamMode = EnableDreamMode,
            EnableDreamSeriesEvents = EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
            EnableNeowExtraOption = EnableNeowExtraOption,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonaMode = StartingPersonaMode,
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
            EnableLucidDreamHarmlessWhisper = EnableLucidDreamHarmlessWhisper
        };
    }

    public static LobbyGameplaySettingsSnapshot FromPersistent(ReAstralPartyModSettings settings)
    {
        return new LobbyGameplaySettingsSnapshot
        {
            EnableStartingInitialPoint = settings.EnableStartingInitialPoint,
            EnableStartingPersonaSelection = settings.EnableStartingPersonaSelection,
            // Dream Mode is temporarily locked and must not propagate from persisted settings.
            EnableDreamMode = false,
            EnableDreamSeriesEvents = settings.EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = settings.EnableEnigmaticSeriesEvents,
            EnableNeowExtraOption = settings.EnableNeowExtraOption,
            NeowExtraOptionSelectionMode = settings.NeowExtraOptionSelectionMode,
            EnableAllPersonas = settings.EnableAllPersonas,
            EnableAllVariantPersonas = settings.EnableAllVariantPersonas,
            EnableExtremeMode = settings.EnableExtremeMode,
            StartingPersonaMode = ReAstralPartyModSettingsManager.ResolveStartingPersonaMode(settings),
            TokenSeriesMode = ReAstralPartyModSettingsManager.ResolveTokenSeriesMode(settings)
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

    public static void UpdateLocalLobbySnapshot(Action<LobbyGameplaySettingsSnapshot> mutator)
    {
        ArgumentNullException.ThrowIfNull(mutator);

        LobbyGameplaySettingsSnapshot updated;
        lock (Gate)
        {
            updated = (_currentSnapshot ?? BuildFallbackSnapshot()).Clone();
            mutator(updated);
            _currentSnapshot = updated.Clone();
        }

        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot updated: start_initial_point={updated.EnableStartingInitialPoint}, start_persona_selection={updated.EnableStartingPersonaSelection}, dream_mode={updated.EnableDreamMode}, dream_series={updated.EnableDreamSeriesEvents}, enigmatic_series={updated.EnableEnigmaticSeriesEvents}, neow_extra_option={updated.EnableNeowExtraOption}, neow_extra_selection={updated.NeowExtraOptionSelectionMode}, all_personas={updated.EnableAllPersonas}, all_variants={updated.EnableAllVariantPersonas}, extreme_mode={updated.EnableExtremeMode}, persona_mode={updated.StartingPersonaMode}, token_series={updated.TokenSeriesMode}");
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
            $"{MainFile.ModId} lobby gameplay snapshot broadcast by host: start_initial_point={snapshot.EnableStartingInitialPoint}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_mode={snapshot.EnableDreamMode}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, neow_extra_option={snapshot.EnableNeowExtraOption}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonaMode}, token_series={snapshot.TokenSeriesMode}");
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
                MainFile.Logger.Warn(
                    $"{MainFile.ModId} lobby gameplay snapshot rejected from non-host sender {senderId} (expected_host={hostNetId}).");
                return;
            }
        }

        var snapshot = message.ToSnapshot();
        SetSnapshotInternal(snapshot, $"remote_snapshot_from_{senderId}", true);
        MainFile.Logger.Info(
            $"{MainFile.ModId} lobby gameplay snapshot received from {senderId}: start_initial_point={snapshot.EnableStartingInitialPoint}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_mode={snapshot.EnableDreamMode}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, neow_extra_option={snapshot.EnableNeowExtraOption}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonaMode}, token_series={snapshot.TokenSeriesMode}");
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
            $"{MainFile.ModId} lobby gameplay snapshot stored ({reason}): start_initial_point={snapshot.EnableStartingInitialPoint}, start_persona_selection={snapshot.EnableStartingPersonaSelection}, dream_mode={snapshot.EnableDreamMode}, dream_series={snapshot.EnableDreamSeriesEvents}, enigmatic_series={snapshot.EnableEnigmaticSeriesEvents}, neow_extra_option={snapshot.EnableNeowExtraOption}, neow_extra_selection={snapshot.NeowExtraOptionSelectionMode}, all_personas={snapshot.EnableAllPersonas}, all_variants={snapshot.EnableAllVariantPersonas}, extreme_mode={snapshot.EnableExtremeMode}, persona_mode={snapshot.StartingPersonaMode}, token_series={snapshot.TokenSeriesMode}");
        if (invokeEvent)
            SnapshotChanged?.Invoke(snapshot.Clone());
    }

    private static void UnregisterHandlersLocked(INetGameService netService)
    {
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsSnapshotMessage>(HandleSnapshotMessage);
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsRequestMessage>(HandleRequestMessage);
    }
}

public struct AstralLobbyGameplaySettingsSnapshotMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 8;

    public bool EnableStartingInitialPoint { get; set; }
    public bool EnableStartingPersonaSelection { get; set; }
    public bool EnableDreamMode { get; set; }
    public bool EnableDreamSeriesEvents { get; set; }
    public bool EnableEnigmaticSeriesEvents { get; set; }
    public bool EnableNeowExtraOption { get; set; }
    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; }
    public bool EnableAllPersonas { get; set; }
    public bool EnableAllVariantPersonas { get; set; }
    public bool EnableExtremeMode { get; set; }
    public StartingPersonaMode StartingPersonaMode { get; set; }
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

    public AstralLobbyGameplaySettingsSnapshotMessage(LobbyGameplaySettingsSnapshot snapshot)
    {
        EnableStartingInitialPoint = snapshot.EnableStartingInitialPoint;
        EnableStartingPersonaSelection = snapshot.EnableStartingPersonaSelection;
        EnableDreamMode = snapshot.EnableDreamMode;
        EnableDreamSeriesEvents = snapshot.EnableDreamSeriesEvents;
        EnableEnigmaticSeriesEvents = snapshot.EnableEnigmaticSeriesEvents;
        EnableNeowExtraOption = snapshot.EnableNeowExtraOption;
        NeowExtraOptionSelectionMode = snapshot.NeowExtraOptionSelectionMode;
        EnableAllPersonas = snapshot.EnableAllPersonas;
        EnableAllVariantPersonas = snapshot.EnableAllVariantPersonas;
        EnableExtremeMode = snapshot.EnableExtremeMode;
        StartingPersonaMode = snapshot.StartingPersonaMode;
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
    }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteBool(EnableStartingInitialPoint);
        writer.WriteBool(EnableStartingPersonaSelection);
        writer.WriteBool(EnableDreamMode);
        writer.WriteBool(EnableDreamSeriesEvents);
        writer.WriteBool(EnableEnigmaticSeriesEvents);
        writer.WriteBool(EnableNeowExtraOption);
        writer.WriteEnum(NeowExtraOptionSelectionMode);
        writer.WriteBool(EnableAllPersonas);
        writer.WriteBool(EnableAllVariantPersonas);
        writer.WriteBool(EnableExtremeMode);
        writer.WriteEnum(StartingPersonaMode);
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
    }

    public void Deserialize(PacketReader reader)
    {
        var schemaVersion = reader.ReadInt();
        if (schemaVersion >= 8)
        {
            EnableStartingInitialPoint = reader.ReadBool();
            EnableStartingPersonaSelection = reader.ReadBool();
            EnableDreamMode = reader.ReadBool();
        }
        else if (schemaVersion >= 2)
        {
            EnableStartingInitialPoint = reader.ReadBool();
            EnableStartingPersonaSelection = reader.ReadBool();
            EnableDreamMode = false;
        }
        else
        {
            EnableStartingInitialPoint = false;
            EnableStartingPersonaSelection = true;
            EnableDreamMode = false;
        }

        if (schemaVersion >= 3)
        {
            EnableDreamSeriesEvents = reader.ReadBool();
            EnableEnigmaticSeriesEvents = reader.ReadBool();
            EnableNeowExtraOption = reader.ReadBool();
        }
        else
        {
            EnableDreamSeriesEvents = true;
            EnableEnigmaticSeriesEvents = true;
            EnableNeowExtraOption = true;
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
        EnableAllVariantPersonas = reader.ReadBool();
        EnableExtremeMode = reader.ReadBool();
        StartingPersonaMode = reader.ReadEnum<StartingPersonaMode>();
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
    }

    public LobbyGameplaySettingsSnapshot ToSnapshot()
    {
        return new LobbyGameplaySettingsSnapshot
        {
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableDreamMode = EnableDreamMode,
            EnableDreamSeriesEvents = EnableDreamSeriesEvents,
            EnableEnigmaticSeriesEvents = EnableEnigmaticSeriesEvents,
            EnableNeowExtraOption = EnableNeowExtraOption,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonaMode = StartingPersonaMode,
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
            EnableLucidDreamHarmlessWhisper = EnableLucidDreamHarmlessWhisper
        };
    }
}

public struct AstralLobbyGameplaySettingsRequestMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 1;

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
    }

    public void Deserialize(PacketReader reader)
    {
        _ = reader.ReadInt();
    }
}
