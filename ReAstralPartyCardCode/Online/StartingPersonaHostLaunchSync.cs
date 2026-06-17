using System;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class StartingPersonaHostLaunchSync
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
                _registeredNetService.UnregisterMessageHandler<StartingPersonaHostLaunchMessage>(HandleLaunchMessage);

            netService.RegisterMessageHandler<StartingPersonaHostLaunchMessage>(HandleLaunchMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info("[StartingPersonaHostLaunchSync] Registered host-launch message handler.");
    }

    public static void Unregister()
    {
        lock (Gate)
        {
            if (_registeredNetService == null)
                return;

            _registeredNetService.UnregisterMessageHandler<StartingPersonaHostLaunchMessage>(HandleLaunchMessage);
            _registeredNetService = null;
        }
    }

    public static void BroadcastLaunch(RunState runState)
    {
        ArgumentNullException.ThrowIfNull(runState);

        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null || netService.Type != NetGameType.Host || !netService.IsConnected)
            return;

        var runKey = StartingPersonaRelicSelectionPatch.GetRunKey(runState);
        var relicOptionIds = StartingPersonaRelicSelectionPatch.CreateOverlayRelicOptions(runState)
            .Select(relic => (relic.CanonicalInstance?.Id ?? relic.Id).ToString())
            .ToList();
        netService.SendMessage(new StartingPersonaHostLaunchMessage(runKey, relicOptionIds));
        MainFile.Logger.Info(
            $"[StartingPersonaHostLaunchSync] Host broadcast persona-selection launch | runKey={runKey} options={relicOptionIds.Count}.");
    }

    private static void HandleLaunchMessage(StartingPersonaHostLaunchMessage message, ulong senderId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return;

        var role = LobbyGameplayNetRoleHelper.GetCurrentRole(netService);
        if (role != LobbyGameplayNetRole.Client)
            return;

        var expectedHostNetId = LobbyGameplayNetRoleHelper.GetHostNetId(netService);
        if (expectedHostNetId == 0UL || senderId != expectedHostNetId)
        {
            MainFile.Logger.Warn(
                $"[StartingPersonaHostLaunchSync] Rejected host-launch from non-host sender | runKey={message.RunKey} sender={senderId} expectedHost={expectedHostNetId}.");
            return;
        }

        MainFile.Logger.Info(
            $"[StartingPersonaHostLaunchSync] Client received host-launch | runKey={message.RunKey} sender={senderId}.");
        _ = StartingPersonaNeowReadyFlow.HandleReadyLaunchAsync(
            message.RunKey,
            "host_broadcast",
            message.RelicOptionIds);
    }
}

public struct StartingPersonaHostLaunchMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 2;

    public string RunKey { get; set; }
    public List<string> RelicOptionIds { get; set; }

    public StartingPersonaHostLaunchMessage(string runKey, List<string>? relicOptionIds = null)
    {
        RunKey = runKey;
        RelicOptionIds = relicOptionIds ?? [];
    }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteString(RunKey);
        writer.WriteInt(RelicOptionIds.Count);
        foreach (var relicOptionId in RelicOptionIds)
            writer.WriteString(relicOptionId);
    }

    public void Deserialize(PacketReader reader)
    {
        var version = reader.ReadInt();
        RunKey = reader.ReadString();
        RelicOptionIds = [];
        if (version < 2)
            return;

        var count = reader.ReadInt();
        for (var i = 0; i < count; i++)
            RelicOptionIds.Add(reader.ReadString());
    }
}
