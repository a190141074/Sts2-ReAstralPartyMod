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

internal static class StartingPersonHostLaunchSync
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
                _registeredNetService.UnregisterMessageHandler<StartingPersonHostLaunchMessage>(HandleLaunchMessage);

            netService.RegisterMessageHandler<StartingPersonHostLaunchMessage>(HandleLaunchMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info("[StartingPersonHostLaunchSync] Registered host-launch message handler.");
    }

    public static void Unregister()
    {
        lock (Gate)
        {
            if (_registeredNetService == null)
                return;

            _registeredNetService.UnregisterMessageHandler<StartingPersonHostLaunchMessage>(HandleLaunchMessage);
            _registeredNetService = null;
        }
    }

    public static void BroadcastLaunch(RunState runState, IReadOnlyList<string> serializedRelicOptionIds)
    {
        ArgumentNullException.ThrowIfNull(runState);
        ArgumentNullException.ThrowIfNull(serializedRelicOptionIds);

        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null || netService.Type != NetGameType.Host || !netService.IsConnected)
            return;

        var runKey = StartingPersonRelicSelectionPatch.GetRunKey(runState);
        var relicOptionIds = serializedRelicOptionIds.ToList();
        netService.SendMessage(new StartingPersonHostLaunchMessage(runKey, relicOptionIds));
        MainFile.Logger.Info(
            $"[StartingPersonHostLaunchSync] Host broadcast persona-selection launch | runKey={runKey} options={relicOptionIds.Count} ids={FormatPayloadSummary(relicOptionIds)}.");
    }

    private static void HandleLaunchMessage(StartingPersonHostLaunchMessage message, ulong senderId)
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
                $"[StartingPersonHostLaunchSync] Rejected host-launch from non-host sender | runKey={message.RunKey} sender={senderId} expectedHost={expectedHostNetId}.");
            return;
        }

        MainFile.Logger.Info(
            $"[StartingPersonHostLaunchSync] Client received host-launch | runKey={message.RunKey} sender={senderId} options={message.RelicOptionIds.Count} ids={FormatPayloadSummary(message.RelicOptionIds)}.");
        _ = StartingPersonNeowReadyFlow.HandleReadyLaunchAsync(
            message.RunKey,
            "host_broadcast",
            message.RelicOptionIds);
    }

    private static string FormatPayloadSummary(IReadOnlyList<string> serializedRelicOptionIds)
    {
        if (serializedRelicOptionIds.Count == 0)
            return "<empty>";

        const int limit = 4;
        var preview = string.Join(", ", serializedRelicOptionIds.Take(limit));
        return serializedRelicOptionIds.Count > limit
            ? $"{preview}, ... ({serializedRelicOptionIds.Count} total)"
            : preview;
    }
}

public struct StartingPersonHostLaunchMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 2;

    public string RunKey { get; set; }
    public List<string> RelicOptionIds { get; set; }

    public StartingPersonHostLaunchMessage(string runKey, List<string>? relicOptionIds = null)
    {
        RunKey = runKey;
        RelicOptionIds = relicOptionIds ?? [];
    }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

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
