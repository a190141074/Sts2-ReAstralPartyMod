using System;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class DevConsoleCommandNotificationSync
{
    private const int MaxCommandLength = 160;
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
                _registeredNetService.UnregisterMessageHandler<DevConsoleCommandNotificationMessage>(HandleMessage);

            netService.RegisterMessageHandler<DevConsoleCommandNotificationMessage>(HandleMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info("[DevConsoleCommandNotificationSync] Registered dev-console notification handler.");
    }

    public static void Unregister()
    {
        lock (Gate)
        {
            if (_registeredNetService == null)
                return;

            _registeredNetService.UnregisterMessageHandler<DevConsoleCommandNotificationMessage>(HandleMessage);
            _registeredNetService = null;
        }
    }

    public static void NotifySuccessfulLocalCommand(Player player, string commandText)
    {
        ArgumentNullException.ThrowIfNull(player);

        var normalizedCommand = NormalizeCommandText(commandText);
        if (string.IsNullOrWhiteSpace(normalizedCommand))
            return;

        ShowNotification(player.NetId, player.Character.Id.Entry, normalizedCommand);

        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer || !netService.IsConnected)
            return;

        if (!LocalContext.IsMe(player))
            return;

        netService.SendMessage(new DevConsoleCommandNotificationMessage
        {
            PlayerNetId = player.NetId,
            CharacterId = player.Character.Id.Entry,
            CommandText = normalizedCommand
        });

        MainFile.Logger.Info(
            $"[DevConsoleCommandNotificationSync] Broadcast console command notification | player={player.NetId} | character={player.Character.Id.Entry} | command={normalizedCommand}");
    }

    private static void HandleMessage(DevConsoleCommandNotificationMessage message, ulong senderId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return;

        if (senderId == netService.NetId)
            return;

        ShowNotification(message.PlayerNetId, message.CharacterId, NormalizeCommandText(message.CommandText));
        MainFile.Logger.Info(
            $"[DevConsoleCommandNotificationSync] Received console command notification | sender={senderId} | player={message.PlayerNetId} | character={message.CharacterId} | command={message.CommandText}");
    }

    private static void ShowNotification(ulong playerNetId, string? characterId, string commandText)
    {
        if (!ReAstralPartyModSettingsManager.EnableConsoleCommandNotifications)
            return;

        var title = new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_NOTIFICATION.console_command.title")
            .GetRawText();
        var body = new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_NOTIFICATION.console_command.body");
        body.Add("PlayerName", ResolvePlayerName(playerNetId));
        body.Add("Character", ResolveCharacterTitle(characterId));
        body.Add("Command", commandText);

        AstralNotificationService.ShowInfo(
            AstralNotificationModule.Multiplayer,
            body.GetFormattedText(),
            title);
    }

    private static string ResolvePlayerName(ulong playerNetId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return playerNetId.ToString();

        var playerName = PlatformUtil.GetPlayerName(netService.Platform, playerNetId);
        return string.IsNullOrWhiteSpace(playerName) ? playerNetId.ToString() : playerName;
    }

    private static string ResolveCharacterTitle(string? characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
            return "<unknown>";

        var loc = LocString.GetIfExists("characters", $"{characterId}.title");
        if (loc == null)
            return characterId;

        var title = loc.GetRawText();
        return string.IsNullOrWhiteSpace(title) ? characterId : title;
    }

    private static string NormalizeCommandText(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return string.Empty;

        var normalized = commandText
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        if (normalized.Length <= MaxCommandLength)
            return normalized;

        return normalized[..(MaxCommandLength - 3)] + "...";
    }
}

internal struct DevConsoleCommandNotificationMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 1;

    public ulong PlayerNetId { get; set; }

    public string CharacterId { get; set; }

    public string CommandText { get; set; }

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteULong(PlayerNetId);
        writer.WriteString(CharacterId ?? string.Empty);
        writer.WriteString(CommandText ?? string.Empty);
    }

    public void Deserialize(PacketReader reader)
    {
        _ = reader.ReadInt();
        PlayerNetId = reader.ReadULong();
        CharacterId = reader.ReadString();
        CommandText = reader.ReadString();
    }
}
