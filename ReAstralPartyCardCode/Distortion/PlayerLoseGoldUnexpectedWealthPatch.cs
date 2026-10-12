using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Distortion;

public sealed class PlayerLoseGoldUnexpectedWealthPatch : IPatchMethod
{
    public static string PatchId => "player_lose_gold_unexpected_wealth";

    public static string Description => "Grant bonus gold from Unexpected Wealth after spending";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new ModPatchTarget(typeof(PlayerCmd), nameof(PlayerCmd.LoseGold))];
    }

    public static void Postfix(decimal amount, Player player, GoldLossType goldLossType, ref Task __result)
    {
        __result = RunAfterLoseGold(__result, amount, player, goldLossType);
    }

    private static async Task RunAfterLoseGold(
        Task originalTask,
        decimal amount,
        Player player,
        GoldLossType goldLossType)
    {
        await originalTask;

        if (player?.RunState is not RunState runState)
            return;
        if (goldLossType != GoldLossType.Spent || amount <= 0m)
            return;
        if (!FinalArenaSeriesHelper.HasComponent(runState, FinalArenaDistortionComponent.UnexpectedWealth))
            return;

        var sequence = FinalArenaSeriesHelper.AdvanceUnexpectedWealthTriggerSequence(runState, player.NetId);
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            MainFile.ModId,
            "unexpected_wealth",
            runState.Rng.StringSeed,
            player.NetId,
            runState.TotalFloor,
            player.Gold,
            amount,
            sequence);
        if (roll >= 30)
            return;

        await PlayerCmd.GainGold(16m, player);
    }
}
