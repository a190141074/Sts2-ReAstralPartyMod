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

        _ = ApplyHostStateAsync(runState, message);
    }

    private static async Task ApplyHostStateAsync(
        RunState runState,
        FinalArenaDistortionComponentMessage message)
    {
        try
        {
            FinalArenaSeriesHelper.ApplySyncedComponents(
                runState,
                message.GetComponents(),
                message.HumanAiDuelApplied,
                message.GetHumanAiDuelGrantedPlayerNetIds(),
                message.GetHumanAiDuelRecordedMindStacks(),
                message.RobberyGoldGranted,
                message.ThreeThousandAstrolabeGoldGranted,
                message.RobberyNextCombatPending,
                message.MysteriousTreasureInitialized,
                message.MysteriousTreasureNextCombatPending,
                message.MysteriousTreasureCombatCounter,
                message.ArbitrageLastFloor,
                message.GlobalPlayerTurnCounter);

            var grantedPlayerNetIds = message.GetHumanAiDuelGrantedPlayerNetIds();
            MainFile.Logger.Info(
                $"[FinalArenaDistortionComponentSync] Applied host state | components={string.Join(',', message.GetComponents())} human_ai_duel_grants=[{string.Join(',', grantedPlayerNetIds)}].");
            await FinalArenaSeriesHelper.ReconcileSyncedHumanAiDuelAsync(
                runState,
                grantedPlayerNetIds,
                message.GetHumanAiDuelRecordedMindStacks());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"[FinalArenaDistortionComponentSync] Failed to apply host state: {ex}");
        }
    }
}

internal struct FinalArenaDistortionComponentMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 3;

    private List<FinalArenaDistortionComponent> _components;
    private List<ulong> _humanAiDuelGrantedPlayerNetIds;
    private Dictionary<ulong, int> _humanAiDuelRecordedMindStacks;

    public FinalArenaDistortionComponentMessage(
        IReadOnlyList<FinalArenaDistortionComponent> components,
        FinalArenaSeriesRunData runData)
    {
        _components = [.. components];
        HumanAiDuelApplied = runData.HumanAiDuelApplied;
        _humanAiDuelGrantedPlayerNetIds = [.. runData.HumanAiDuelGrantedPlayerNetIds];
        _humanAiDuelRecordedMindStacks = new(runData.HumanAiDuelRecordedMindStacks);
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

    public IReadOnlyList<ulong> GetHumanAiDuelGrantedPlayerNetIds()
    {
        return _humanAiDuelGrantedPlayerNetIds ?? [];
    }

    public IReadOnlyDictionary<ulong, int> GetHumanAiDuelRecordedMindStacks()
    {
        return _humanAiDuelRecordedMindStacks ?? new Dictionary<ulong, int>();
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteInt((_components ?? []).Count);
        foreach (var component in _components ?? [])
            writer.WriteEnum(component);

        writer.WriteBool(HumanAiDuelApplied);
        writer.WriteInt((_humanAiDuelGrantedPlayerNetIds ?? []).Count);
        foreach (var playerNetId in _humanAiDuelGrantedPlayerNetIds ?? [])
            writer.WriteULong(playerNetId);
        writer.WriteInt((_humanAiDuelRecordedMindStacks ?? []).Count);
        foreach (var entry in (_humanAiDuelRecordedMindStacks ?? new Dictionary<ulong, int>()).OrderBy(entry => entry.Key))
        {
            writer.WriteULong(entry.Key);
            writer.WriteInt(entry.Value);
        }
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
        var grantedCount = reader.ReadInt();
        _humanAiDuelGrantedPlayerNetIds = new List<ulong>(Math.Max(0, grantedCount));
        for (var i = 0; i < grantedCount; i++)
            _humanAiDuelGrantedPlayerNetIds.Add(reader.ReadULong());

        var recordedMindCount = reader.ReadInt();
        _humanAiDuelRecordedMindStacks = new Dictionary<ulong, int>(Math.Max(0, recordedMindCount));
        for (var i = 0; i < recordedMindCount; i++)
            _humanAiDuelRecordedMindStacks[reader.ReadULong()] = reader.ReadInt();
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
