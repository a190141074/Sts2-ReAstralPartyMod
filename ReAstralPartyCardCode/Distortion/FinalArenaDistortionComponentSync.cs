using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

internal static class FinalArenaDistortionComponentSync
{
    private static readonly object Gate = new();
    private static INetGameService? _registeredNetService;

    public static void Register(INetGameService? netService = null)
    {
        netService ??= RunManager.Instance?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return;

        lock (Gate)
        {
            if (ReferenceEquals(_registeredNetService, netService))
                return;

            if (_registeredNetService != null)
                _registeredNetService.UnregisterMessageHandler<FinalArenaDistortionComponentMessage>(HandleMessage);

            netService.RegisterMessageHandler<FinalArenaDistortionComponentMessage>(HandleMessage);
            _registeredNetService = netService;
        }
    }

    public static void BroadcastState(RunState runState)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null ||
            netService.Type != NetGameType.Host ||
            !netService.IsConnected)
            return;

        if (!FinalArenaSeriesHelper.TryGetRunDataSnapshot(runState, out var components, out var runData))
            return;

        netService.SendMessage(new FinalArenaDistortionComponentMessage(components, runData));
    }

    private static void HandleMessage(FinalArenaDistortionComponentMessage message, ulong senderId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return;

        var hostNetId = LobbyGameplayNetRoleHelper.GetHostNetId(netService);
        if (hostNetId == 0UL || senderId != hostNetId)
        {
            MainFile.Logger.Warn(
                $"[FinalArenaDistortionComponentSync] Rejected non-host payload from {senderId} (expected {hostNetId}).");
            return;
        }

        if (RunManager.Instance?.DebugOnlyGetState() is not RunState runState)
            return;

        FinalArenaSeriesHelper.ApplySyncedComponents(
            runState,
            message.GetComponents(),
            message.HumanAiDuelApplied,
            message.RobberyGoldGranted,
            message.ThreeThousandAstrolabeGoldGranted,
            message.RobberyNextCombatPending,
            message.MysteriousTreasureInitialized,
            message.MysteriousTreasureNextCombatPending,
            message.MysteriousTreasureCombatCounter,
            message.ArbitrageLastFloor,
            message.GlobalPlayerTurnCounter);
    }
}

internal struct FinalArenaDistortionComponentMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 2;

    private List<FinalArenaDistortionComponent> _components;

    public FinalArenaDistortionComponentMessage(
        IReadOnlyList<FinalArenaDistortionComponent> components,
        FinalArenaSeriesRunData runData)
    {
        _components = [.. components];
        HumanAiDuelApplied = runData.HumanAiDuelApplied;
        RobberyGoldGranted = runData.RobberyGoldGranted;
        ThreeThousandAstrolabeGoldGranted = runData.ThreeThousandAstrolabeGoldGranted;
        RobberyNextCombatPending = runData.RobberyNextCombatStunPending;
        MysteriousTreasureInitialized = runData.MysteriousTreasureInitialized;
        MysteriousTreasureNextCombatPending = runData.MysteriousTreasureNextCombatPending;
        MysteriousTreasureCombatCounter = runData.MysteriousTreasureCombatCounter;
        ArbitrageLastFloor = runData.ArbitrageLastFloor;
        GlobalPlayerTurnCounter = runData.GlobalPlayerTurnCounter;
    }

    public bool HumanAiDuelApplied { get; private set; }
    public bool RobberyGoldGranted { get; private set; }
    public bool ThreeThousandAstrolabeGoldGranted { get; private set; }
    public bool RobberyNextCombatPending { get; private set; }
    public bool MysteriousTreasureInitialized { get; private set; }
    public bool MysteriousTreasureNextCombatPending { get; private set; }
    public int MysteriousTreasureCombatCounter { get; private set; }
    public int ArbitrageLastFloor { get; private set; }
    public int GlobalPlayerTurnCounter { get; private set; }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public IReadOnlyList<FinalArenaDistortionComponent> GetComponents()
    {
        return _components ?? [];
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteInt((_components ?? []).Count);
        foreach (var component in _components ?? [])
            writer.WriteEnum(component);

        writer.WriteBool(HumanAiDuelApplied);
        writer.WriteBool(RobberyGoldGranted);
        writer.WriteBool(ThreeThousandAstrolabeGoldGranted);
        writer.WriteBool(RobberyNextCombatPending);
        writer.WriteBool(MysteriousTreasureInitialized);
        writer.WriteBool(MysteriousTreasureNextCombatPending);
        writer.WriteInt(MysteriousTreasureCombatCounter);
        writer.WriteInt(ArbitrageLastFloor);
        writer.WriteInt(GlobalPlayerTurnCounter);
    }

    public void Deserialize(PacketReader reader)
    {
        var schemaVersion = reader.ReadInt();
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException($"{MainFile.ModId} received unsupported Final Arena distortion schema {schemaVersion}.");

        var count = reader.ReadInt();
        _components = new List<FinalArenaDistortionComponent>(Math.Max(0, count));
        for (var i = 0; i < count; i++)
        {
            var component = reader.ReadEnum<FinalArenaDistortionComponent>();
            if (Enum.IsDefined(component))
                _components.Add(component);
        }

        HumanAiDuelApplied = reader.ReadBool();
        RobberyGoldGranted = reader.ReadBool();
        ThreeThousandAstrolabeGoldGranted = reader.ReadBool();
        RobberyNextCombatPending = reader.ReadBool();
        MysteriousTreasureInitialized = reader.ReadBool();
        MysteriousTreasureNextCombatPending = reader.ReadBool();
        MysteriousTreasureCombatCounter = reader.ReadInt();
        ArbitrageLastFloor = reader.ReadInt();
        GlobalPlayerTurnCounter = reader.ReadInt();
    }
}
