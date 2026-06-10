using System;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class LucidDreamNotificationSync
{
    private const int ToggleNotificationNumber = 101;
    private const int MaxSettingTitleLength = 80;
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
                _registeredNetService.UnregisterMessageHandler<LucidDreamToggleNotificationMessage>(HandleMessage);

            netService.RegisterMessageHandler<LucidDreamToggleNotificationMessage>(HandleMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info("[LucidDreamNotificationSync] Registered lucid-dream notification handler.");
    }

    public static void NotifyLocalToggle(StartRunLobby? lobby, string settingTitle, bool enabled)
    {
        if (lobby == null)
            return;

        var localPlayer = lobby.LocalPlayer;
        var playerNetId = localPlayer.id;
        var characterId = localPlayer.character?.Id.Entry ?? string.Empty;
        var normalizedTitle = NormalizeSettingTitle(settingTitle);

        ShowNotification(playerNetId, characterId, normalizedTitle, enabled);

        var netService = lobby.NetService ?? _registeredNetService ?? RunManager.Instance?.NetService;
        Register(netService);
        if (netService == null || netService.Type is NetGameType.None or NetGameType.Singleplayer || !netService.IsConnected)
            return;
        if (LobbyGameplayNetRoleHelper.GetCurrentRole(lobby) != LobbyGameplayNetRole.Host)
            return;

        netService.SendMessage(new LucidDreamToggleNotificationMessage
        {
            PlayerNetId = playerNetId,
            CharacterId = characterId,
            SettingTitle = normalizedTitle,
            Enabled = enabled
        });

        MainFile.Logger.Info(
            $"[LucidDreamNotificationSync] Broadcast lucid-dream toggle notification | player={playerNetId} | character={characterId} | setting={normalizedTitle} | enabled={enabled}");
    }

    private static void HandleMessage(LucidDreamToggleNotificationMessage message, ulong senderId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return;
        if (senderId == netService.NetId)
            return;

        ShowNotification(
            message.PlayerNetId,
            message.CharacterId,
            NormalizeSettingTitle(message.SettingTitle),
            message.Enabled);
        MainFile.Logger.Info(
            $"[LucidDreamNotificationSync] Received lucid-dream toggle notification | sender={senderId} | player={message.PlayerNetId} | character={message.CharacterId} | setting={message.SettingTitle} | enabled={message.Enabled}");
    }

    private static void ShowNotification(ulong playerNetId, string? characterId, string settingTitle, bool enabled)
    {
        var body = new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_NOTIFICATION.lucid_dream_toggle.body");
        body.Add("PlayerName", ResolvePlayerName(playerNetId));
        body.Add("Character", ResolveCharacterTitle(characterId));
        body.Add("Setting", settingTitle);
        body.Add("State", GetToggleStateText(enabled));

        AstralNotificationService.ShowDiagnosticInfo(
            AstralNotificationModule.Multiplayer,
            AstralNotificationArea.LucidDreamDiagnostics,
            ToggleNotificationNumber,
            body.GetFormattedText(),
            new LocString("settings_ui", "RE_ASTRAL_PARTY_MOD_NOTIFICATION.lucid_dream_toggle.stage").GetRawText());
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

    private static string GetToggleStateText(bool enabled)
    {
        return new LocString(
                "settings_ui",
                enabled
                    ? "RE_ASTRAL_PARTY_MOD_NOTIFICATION.lucid_dream_toggle.state_enabled"
                    : "RE_ASTRAL_PARTY_MOD_NOTIFICATION.lucid_dream_toggle.state_disabled")
            .GetRawText();
    }

    private static string NormalizeSettingTitle(string? settingTitle)
    {
        if (string.IsNullOrWhiteSpace(settingTitle))
            return "<unknown>";

        var normalized = settingTitle
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Trim();
        if (normalized.Length <= MaxSettingTitleLength)
            return normalized;

        return normalized[..(MaxSettingTitleLength - 3)] + "...";
    }
}

internal struct LucidDreamToggleNotificationMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 1;

    public ulong PlayerNetId { get; set; }

    public string CharacterId { get; set; }

    public string SettingTitle { get; set; }

    public bool Enabled { get; set; }

    public bool ShouldBroadcast => true;

    public NetTransferMode Mode => NetTransferMode.Reliable;

    public LogLevel LogLevel => LogLevel.Debug;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteULong(PlayerNetId);
        writer.WriteString(CharacterId ?? string.Empty);
        writer.WriteString(SettingTitle ?? string.Empty);
        writer.WriteBool(Enabled);
    }

    public void Deserialize(PacketReader reader)
    {
        _ = reader.ReadInt();
        PlayerNetId = reader.ReadULong();
        CharacterId = reader.ReadString();
        SettingTitle = reader.ReadString();
        Enabled = reader.ReadBool();
    }
}
