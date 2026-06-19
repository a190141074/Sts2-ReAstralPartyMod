using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class EnigmaticUniqueMaterialRewardSync
{
    private static readonly object Gate = new();

    private static INetGameService? _registeredNetService;
    private static RunLocationTargetedMessageBuffer? _registeredMessageBuffer;

    public static void Register()
    {
        var runManager = RunManager.Instance;
        var netService = runManager?.NetService;
        var messageBuffer = runManager?.RunLocationTargetedBuffer;
        if (netService == null || messageBuffer == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return;

        lock (Gate)
        {
            if (ReferenceEquals(_registeredNetService, netService) &&
                ReferenceEquals(_registeredMessageBuffer, messageBuffer))
                return;

            if (_registeredMessageBuffer != null)
                _registeredMessageBuffer.UnregisterMessageHandler<EnigmaticUniqueMaterialRewardSyncMessage>(HandleMessage);

            messageBuffer.RegisterMessageHandler<EnigmaticUniqueMaterialRewardSyncMessage>(HandleMessage);
            _registeredNetService = netService;
            _registeredMessageBuffer = messageBuffer;
        }

        MainFile.Logger.Info("[EnigmaticUniqueMaterialRewardSync] Registered unique material reward sync handler.");
    }

    public static void Unregister()
    {
        lock (Gate)
        {
            if (_registeredMessageBuffer != null)
                _registeredMessageBuffer.UnregisterMessageHandler<EnigmaticUniqueMaterialRewardSyncMessage>(HandleMessage);

            _registeredNetService = null;
            _registeredMessageBuffer = null;
        }
    }

    public static void SyncClaim(Player owner, EnigmaticUniqueMaterialKind kind, int amount)
    {
        if (!TryBuildLocalMessage(owner, kind, amount, EnigmaticUniqueMaterialRewardSyncAction.Claimed, out var message))
            return;

        _registeredNetService!.SendMessage(message);
        MainFile.Logger.Info(
            $"[EnigmaticUniqueMaterialRewardSync] unique material reward sync sent | action=claimed | owner={owner.NetId} | kind={kind} | amount={amount}");
    }

    public static void SyncSkip(Player owner, EnigmaticUniqueMaterialKind kind, int amount)
    {
        if (!TryBuildLocalMessage(owner, kind, amount, EnigmaticUniqueMaterialRewardSyncAction.Skipped, out var message))
            return;

        _registeredNetService!.SendMessage(message);
        MainFile.Logger.Info(
            $"[EnigmaticUniqueMaterialRewardSync] unique material reward sync sent | action=skipped | owner={owner.NetId} | kind={kind} | amount={amount}");
    }

    private static bool TryBuildLocalMessage(
        Player owner,
        EnigmaticUniqueMaterialKind kind,
        int amount,
        EnigmaticUniqueMaterialRewardSyncAction action,
        out EnigmaticUniqueMaterialRewardSyncMessage message)
    {
        message = default;

        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        var messageBuffer = _registeredMessageBuffer ?? RunManager.Instance?.RunLocationTargetedBuffer;
        if (netService == null || messageBuffer == null || netService.Type is NetGameType.None or NetGameType.Singleplayer)
            return false;

        if (!netService.IsConnected)
            return false;

        if (!MegaCrit.Sts2.Core.Context.LocalContext.IsMe(owner))
            return false;

        message = new EnigmaticUniqueMaterialRewardSyncMessage
        {
            Kind = kind,
            Amount = amount,
            Action = action,
            Location = messageBuffer.CurrentLocation
        };
        return true;
    }

    private static void HandleMessage(EnigmaticUniqueMaterialRewardSyncMessage message, ulong senderId)
    {
        var runManager = RunManager.Instance;
        var runState = runManager?.DebugOnlyGetState();
        var player = runState?.GetPlayer(senderId);
        if (player == null)
        {
            MainFile.Logger.Warn(
                $"[EnigmaticUniqueMaterialRewardSync] Dropped reward sync for unknown player | sender={senderId} | action={message.Action} | kind={message.Kind} | amount={message.Amount}");
            return;
        }

        MainFile.Logger.Info(
            $"[EnigmaticUniqueMaterialRewardSync] unique material reward sync received | sender={senderId} | action={message.Action} | kind={message.Kind} | amount={message.Amount}");

        if (message.Action == EnigmaticUniqueMaterialRewardSyncAction.Claimed)
        {
            _ = EnigmaticRewardRegistry.GetConfig(message.Kind).GrantRewardAsync(player, message.Amount);
            return;
        }

        MainFile.Logger.Info(
            $"[EnigmaticUniqueMaterialRewardSync] unique material reward skipped | sender={senderId} | kind={message.Kind} | amount={message.Amount}");
    }
}

internal enum EnigmaticUniqueMaterialRewardSyncAction
{
    Claimed = 0,
    Skipped = 1
}

internal struct EnigmaticUniqueMaterialRewardSyncMessage : INetMessage, IPacketSerializable, IRunLocationTargetedMessage
{
    public EnigmaticUniqueMaterialKind Kind { get; set; }

    public int Amount { get; set; }

    public EnigmaticUniqueMaterialRewardSyncAction Action { get; set; }

    public RunLocation Location { get; set; }

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteEnum(Kind);
        writer.WriteInt(Amount);
        writer.WriteEnum(Action);
        writer.Write(Location);
    }

    public void Deserialize(PacketReader reader)
    {
        Kind = reader.ReadEnum<EnigmaticUniqueMaterialKind>();
        Amount = reader.ReadInt();
        Action = reader.ReadEnum<EnigmaticUniqueMaterialRewardSyncAction>();
        Location = reader.Read<RunLocation>();
    }
}
