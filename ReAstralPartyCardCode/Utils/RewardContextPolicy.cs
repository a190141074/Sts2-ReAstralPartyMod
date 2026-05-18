using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class RewardContextPolicy
{
    public static void LogStartupDiagnostics()
    {
        MainFile.Logger.Info($"[{MainFile.ModId}] Reward context policy ready.");
    }

    public static string DescribeRewardContext(Player owner, string operation)
    {
        var runManager = RunManager.Instance;
        var netType = runManager?.NetService.Type.ToString() ?? "no_net_service";
        var runState = runManager?.DebugOnlyGetState();
        var actIndex = runState?.CurrentActIndex.ToString() ?? "no_run";
        var roomType = runState?.CurrentRoom?.GetType().Name ?? "no_room";
        var local = LocalContext.IsMe(owner);
        return $"operation={operation} local={local} netMode={netType} actIndex={actIndex} room={roomType}";
    }

    public static bool CanUseRewardSynchronizer(Player owner, string operation)
    {
        var runManager = RunManager.Instance;
        if (runManager == null)
        {
            MainFile.Logger.Warn($"Skipped reward sync for {operation}: RunManager unavailable.");
            return false;
        }

        if (!LocalContext.IsMe(owner))
            return false;

        if (runManager.RewardSynchronizer == null)
        {
            MainFile.Logger.Warn(
                $"Skipped reward sync for {operation}: RewardSynchronizer unavailable | {DescribeRewardContext(owner, operation)}");
            return false;
        }

        var netType = runManager.NetService.Type;
        if (netType is NetGameType.None or NetGameType.Singleplayer)
            return true;

        if (!AstralNetPhaseGuard.Guard(AstralNetPhase.MapOrRoom, $"reward sync {operation}"))
        {
            MainFile.Logger.Warn(
                $"Rejected reward sync outside safe phase | {DescribeRewardContext(owner, operation)}");
            return false;
        }

        MainFile.Logger.Warn(
            $"Skipped reward sync for {operation}: deterministic multiplayer path enforced | {DescribeRewardContext(owner, operation)}");
        return false;
    }
}
