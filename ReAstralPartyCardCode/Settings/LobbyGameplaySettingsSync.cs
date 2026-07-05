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

    public bool EnableStartingPersonSelection { get; set; } = true;

    public bool EnableMoonPropShopSlots { get; set; } = true;

    public bool EnableMoonPropRelics { get; set; } = true;

    public bool EnableJewelryRelics { get; set; } = true;

    public bool EnableNeowExtraOption { get; set; } = true;


    public bool EnableCollectorsCards { get; set; } = true;

    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; } =
        NeowExtraOptionSelectionMode.DefaultRandom;

    public bool EnableAllPersons { get; set; }

    public bool EnableVariantPersons { get; set; } = true;

    public bool EnableAllVariantPersons { get; set; }

    public bool EnableExtremeMode { get; set; }

    public StartingPersonMode StartingPersonMode { get; set; } = StartingPersonMode.Standard;

    public TokenSeriesMode TokenSeriesMode { get; set; } = TokenSeriesMode.RandomTwo;








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
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableMoonPropShopSlots = EnableMoonPropShopSlots,
            EnableMoonPropRelics = EnableMoonPropRelics,
            EnableJewelryRelics = EnableJewelryRelics,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonMode = StartingPersonMode,
            TokenSeriesMode = TokenSeriesMode,
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
            EnableStartingPersonaSelection = ReAstralPartyModSettingsManager.EnableStartingPersonaSelection,
            EnableMoonPropShopSlots = ReAstralPartyModSettingsManager.EnableMoonPropShopSlots,
            EnableMoonPropRelics = ReAstralPartyModSettingsManager.EnableMoonPropRelics,
            EnableJewelryRelics = ReAstralPartyModSettingsManager.EnableJewelryRelics,
            EnableNeowExtraOption = ReAstralPartyModSettingsManager.EnableNeowExtraOption,
            EnableCollectorsCards = ReAstralPartyModSettingsManager.EnableCollectorsCards,
            NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
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
        return BuildFallbackSnapshot();
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
            SanitizeSnapshot(updated);
            updated.NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(
                updated.NeowExtraOptionSelectionMode);
            _currentSnapshot = updated.Clone();
        }

        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot updated.");
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
        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot broadcast by host.");
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
        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot received from {senderId}.");
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
        SanitizeSnapshot(snapshot);

        lock (Gate)
        {
            _currentSnapshot = snapshot.Clone();
        }

        MainFile.Logger.Info($"{MainFile.ModId} lobby gameplay snapshot stored ({reason}).");
        if (invokeEvent)
            SnapshotChanged?.Invoke(snapshot.Clone());
    }

    private static void SanitizeSnapshot(LobbyGameplaySettingsSnapshot snapshot)
    {
        snapshot.BannedRelicIdsSerialized =
        [
            .. ReAstralPartyModSettingsManager.SanitizeSerializedBannedRelicIds(snapshot.BannedRelicIdsSerialized)
        ];
    }

    private static void UnregisterHandlersLocked(INetGameService netService)
    {
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsSnapshotMessage>(HandleSnapshotMessage);
        netService.UnregisterMessageHandler<AstralLobbyGameplaySettingsRequestMessage>(HandleRequestMessage);
    }

}

public struct AstralLobbyGameplaySettingsSnapshotMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 19;

    public AstralContentMode CurrentContentMode { get; set; }
    public bool EnableStartingInitialPoint { get; set; }
    public bool EnableStartingAstralRelicStore { get; set; } = true;
    public bool EnableStartingPersonaSelection { get; set; }
    public bool EnableMoonPropShopSlots { get; set; } = true;
    public bool EnableMoonPropRelics { get; set; } = true;
    public bool EnableJewelryRelics { get; set; } = true;
    public bool EnableNeowExtraOption { get; set; }
    public bool EnableCollectorsCards { get; set; } = true;
    public NeowExtraOptionSelectionMode NeowExtraOptionSelectionMode { get; set; }
    public bool EnableAllPersonas { get; set; }
    public bool EnableVariantPersonas { get; set; } = true;
    public bool EnableAllVariantPersonas { get; set; }
    public bool EnableExtremeMode { get; set; }
    public StartingPersonMode StartingPersonMode { get; set; }
    public TokenSeriesMode TokenSeriesMode { get; set; }
    public List<string> BannedRelicIdsSerialized { get; set; }

    public AstralLobbyGameplaySettingsSnapshotMessage(LobbyGameplaySettingsSnapshot snapshot)
    {
        CurrentContentMode = snapshot.CurrentContentMode;
        EnableStartingInitialPoint = snapshot.EnableStartingInitialPoint;
        EnableStartingAstralRelicStore = snapshot.EnableStartingAstralRelicStore;
        EnableStartingPersonaSelection = snapshot.EnableStartingPersonaSelection;
        EnableMoonPropShopSlots = snapshot.EnableMoonPropShopSlots;
        EnableMoonPropRelics = snapshot.EnableMoonPropRelics;
        EnableJewelryRelics = snapshot.EnableJewelryRelics;
        EnableNeowExtraOption = snapshot.EnableNeowExtraOption;
        EnableCollectorsCards = snapshot.EnableCollectorsCards;
        NeowExtraOptionSelectionMode = snapshot.NeowExtraOptionSelectionMode;
        EnableAllPersonas = snapshot.EnableAllPersonas;
        EnableVariantPersonas = snapshot.EnableVariantPersonas;
        EnableAllVariantPersonas = snapshot.EnableAllVariantPersonas;
        EnableExtremeMode = snapshot.EnableExtremeMode;
        StartingPersonMode = snapshot.StartingPersonMode;
        TokenSeriesMode = snapshot.TokenSeriesMode;
        BannedRelicIdsSerialized =
        [
            .. ReAstralPartyModSettingsManager.SanitizeSerializedBannedRelicIds(snapshot.BannedRelicIdsSerialized)
        ];
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
        writer.WriteBool(EnableStartingPersonaSelection);
        writer.WriteBool(EnableMoonPropShopSlots);
        writer.WriteBool(EnableMoonPropRelics);
        writer.WriteBool(EnableJewelryRelics);
        writer.WriteBool(EnableNeowExtraOption);
        writer.WriteBool(EnableCollectorsCards);
        writer.WriteEnum(NeowExtraOptionSelectionMode);
        writer.WriteBool(EnableAllPersonas);
        writer.WriteBool(EnableVariantPersonas);
        writer.WriteBool(EnableAllVariantPersonas);
        writer.WriteBool(EnableExtremeMode);
        writer.WriteEnum(StartingPersonMode);
        writer.WriteEnum(TokenSeriesMode);
        writer.WriteInt(BannedRelicIdsSerialized.Count);
        foreach (var bannedRelicId in BannedRelicIdsSerialized)
            writer.WriteString(bannedRelicId);
    }

    public void Deserialize(PacketReader reader)
    {
        var schemaVersion = reader.ReadInt();
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException($"{MainFile.ModId} received unsupported lobby gameplay snapshot schema {schemaVersion}.");

        var rawMode = reader.ReadEnum<AstralContentMode>();
        CurrentContentMode = AstralContentModeRegistry.NormalizeMode(rawMode);
        EnableStartingInitialPoint = reader.ReadBool();
        EnableStartingAstralRelicStore = reader.ReadBool();
        EnableStartingPersonaSelection = reader.ReadBool();
        EnableMoonPropShopSlots = reader.ReadBool();
        EnableMoonPropRelics = reader.ReadBool();
        EnableJewelryRelics = reader.ReadBool();
        EnableNeowExtraOption = reader.ReadBool();
        EnableCollectorsCards = reader.ReadBool();
        var rawNeowMode = reader.ReadEnum<NeowExtraOptionSelectionMode>();
        NeowExtraOptionSelectionMode = ReAstralPartyModSettingsManager.NormalizeNeowExtraOptionSelectionMode(rawNeowMode);

        EnableAllPersonas = reader.ReadBool();
        EnableVariantPersonas = reader.ReadBool();
        EnableAllVariantPersonas = reader.ReadBool();
        EnableExtremeMode = reader.ReadBool();
        StartingPersonMode = reader.ReadEnum<StartingPersonMode>();
        TokenSeriesMode = reader.ReadEnum<TokenSeriesMode>();
        var count = reader.ReadInt();
        var bannedRelicIds = new List<string>(count);
        for (var index = 0; index < count; index++)
            bannedRelicIds.Add(reader.ReadString());
        BannedRelicIdsSerialized = bannedRelicIds;
    }

    public LobbyGameplaySettingsSnapshot ToSnapshot()
    {
        return new LobbyGameplaySettingsSnapshot
        {
            CurrentContentMode = CurrentContentMode,
            EnableStartingInitialPoint = EnableStartingInitialPoint,
            EnableStartingAstralRelicStore = EnableStartingAstralRelicStore,
            EnableStartingPersonaSelection = EnableStartingPersonaSelection,
            EnableMoonPropShopSlots = EnableMoonPropShopSlots,
            EnableMoonPropRelics = EnableMoonPropRelics,
            EnableJewelryRelics = EnableJewelryRelics,
            EnableNeowExtraOption = EnableNeowExtraOption,
            EnableCollectorsCards = EnableCollectorsCards,
            NeowExtraOptionSelectionMode = NeowExtraOptionSelectionMode,
            EnableAllPersonas = EnableAllPersonas,
            EnableVariantPersonas = EnableVariantPersonas,
            EnableAllVariantPersonas = EnableAllVariantPersonas,
            EnableExtremeMode = EnableExtremeMode,
            StartingPersonMode = StartingPersonMode,
            TokenSeriesMode = TokenSeriesMode,
            BannedRelicIdsSerialized =
            [
                .. ReAstralPartyModSettingsManager.SanitizeSerializedBannedRelicIds(BannedRelicIdsSerialized)
            ]
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
