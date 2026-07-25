using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Messages.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Settings;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal static class AstralDlcOverrideSync
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
                _registeredNetService.UnregisterMessageHandler<AstralDlcOverrideMessage>(HandleMessage);

            netService.RegisterMessageHandler<AstralDlcOverrideMessage>(HandleMessage);
            _registeredNetService = netService;
        }

        MainFile.Logger.Info("[AstralDlcOverrideSync] Registered astraldlc override handler.");
    }

    public static void BroadcastOverride(IReadOnlyList<TokenSeriesAvailabilityHelper.TokenSeries> openSeries)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null ||
            netService.Type != NetGameType.Host ||
            !netService.IsConnected)
            return;

        netService.SendMessage(new AstralDlcOverrideMessage(openSeries));
        MainFile.Logger.Info(
            $"[AstralDlcOverrideSync] Broadcast astraldlc override: {TokenSeriesAvailabilityHelper.BuildSeriesListSummary(openSeries)}");
    }

    private static void HandleMessage(AstralDlcOverrideMessage message, ulong senderId)
    {
        var netService = _registeredNetService ?? RunManager.Instance?.NetService;
        if (netService == null)
            return;

        var hostNetId = LobbyGameplayNetRoleHelper.GetHostNetId(netService);
        if (hostNetId == 0UL || senderId != hostNetId)
        {
            MainFile.Logger.Warn(
                $"[AstralDlcOverrideSync] Rejected astraldlc override from non-host sender {senderId} (expected_host={hostNetId}).");
            return;
        }

        if (RunManager.Instance?.DebugOnlyGetState() is not RunState runState)
            return;

        var openSeries = message.GetOpenSeries();
        TokenSeriesAvailabilityHelper.SetManualOpenSeries(runState, openSeries);
        MainFile.Logger.Info(
            $"[AstralDlcOverrideSync] Applied astraldlc override from host {senderId}: {TokenSeriesAvailabilityHelper.BuildSeriesListSummary(openSeries)}");
    }
}

internal struct AstralDlcOverrideMessage : INetMessage, IPacketSerializable
{
    private const int SchemaVersion = 1;

    private List<TokenSeriesAvailabilityHelper.TokenSeries> _openSeries;

    public AstralDlcOverrideMessage(IReadOnlyList<TokenSeriesAvailabilityHelper.TokenSeries> openSeries)
    {
        _openSeries = [.. openSeries];
    }

    public bool ShouldBroadcast => false;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.Debug;
    public bool ShouldBuffer => false;

    public IReadOnlyList<TokenSeriesAvailabilityHelper.TokenSeries> GetOpenSeries()
    {
        return _openSeries ?? [];
    }

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(SchemaVersion);
        writer.WriteInt((_openSeries ?? []).Count);
        foreach (var series in _openSeries ?? [])
            writer.WriteEnum(series);
    }

    public void Deserialize(PacketReader reader)
    {
        var schemaVersion = reader.ReadInt();
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException($"{MainFile.ModId} received unsupported astraldlc override schema {schemaVersion}.");

        var count = reader.ReadInt();
        _openSeries = new List<TokenSeriesAvailabilityHelper.TokenSeries>(Math.Max(0, count));
        for (var index = 0; index < count; index++)
        {
            var series = reader.ReadEnum<TokenSeriesAvailabilityHelper.TokenSeries>();
            if (Enum.IsDefined(series))
                _openSeries.Add(series);
        }
    }
}
