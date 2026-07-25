using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib;
using STS2RitsuLib.RunData;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class FinalArenaSeriesHelper
{
    private const string StunGunGrantedKey = "final_arena_stun_gun_granted";

    private static RunSavedData<FinalArenaSeriesRunData> _runData = null!;
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        using (RitsuLibFramework.BeginModDataRegistration(MainFile.ModId))
        {
            _runData = RitsuLibFramework
                .GetRunSavedDataStore(MainFile.ModId)
                .Register(
                    StunGunGrantedKey,
                    static () => new FinalArenaSeriesRunData(),
                    new RunSavedDataOptions
                    {
                        WritePolicy = RunSavedDataWritePolicy.WhenNonDefault
                    });
        }

        _registered = true;
    }

    public static async Task GrantStunGunIfNeeded(RunState runState)
    {
        if (!_registered)
            Register();

        if (!TokenSeriesAvailabilityHelper.TryGetState(runState, out var state) ||
            !state.OpenSeries.Contains(TokenSeriesAvailabilityHelper.TokenSeries.FinalArena))
            return;

        if (_runData.TryGet(runState, out var runData) && runData?.StunGunGranted == true)
            return;

        var stunGun = ModelDb.Relic<TokenExclusiveStunGun>().CanonicalInstance ?? ModelDb.Relic<TokenExclusiveStunGun>();
        foreach (var player in runState.Players.OrderBy(static player => player.NetId))
            await GrantStunGunToPlayerIfMissing(player, stunGun);

        _runData.Set(runState, new FinalArenaSeriesRunData { StunGunGranted = true });
    }

    private static async Task GrantStunGunToPlayerIfMissing(Player player, RelicModel canonicalStunGun)
    {
        if (player.GetRelic<TokenExclusiveStunGun>() != null)
            return;

        try
        {
            var obtained = await RelicCmd.Obtain(canonicalStunGun.ToMutable(), player);
            AstralTelemetry.RecordObtainedToken(player, obtained);
            MainFile.Logger.Info(
                $"{MainFile.ModId} granted Final Arena stun gun to player {player.NetId}.");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error(
                $"{MainFile.ModId} failed to grant Final Arena stun gun to player {player.NetId}: {ex}");
            throw;
        }
    }
}

internal sealed class FinalArenaSeriesRunData
{
    public bool StunGunGranted { get; set; }
}
