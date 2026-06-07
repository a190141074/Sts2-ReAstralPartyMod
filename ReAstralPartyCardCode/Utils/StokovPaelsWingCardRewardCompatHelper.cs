using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StokovPaelsWingCardRewardCompatHelper
{
    private static readonly Func<CardReward, CardCreationOptions>? GetOptions = ResolveOptionsGetter();

    public static bool ShouldDropSkipForStokovPaelsWing(
        CardReward reward,
        IReadOnlyList<CardRewardAlternative> alternatives)
    {
        if (alternatives.Count <= 2)
            return false;

        var player = reward.Player;
        if (player.GetRelic<VariantPersonStokov>() == null || player.GetRelic<PaelsWing>() == null)
            return false;

        return alternatives.Any(static option => string.Equals(option.OptionId, "Skip", StringComparison.Ordinal))
               && alternatives.Any(static option => string.Equals(option.OptionId, "REROLL", StringComparison.Ordinal))
               && alternatives.Any(static option => string.Equals(option.OptionId, "SACRIFICE", StringComparison.Ordinal));
    }

    public static bool TryDropSkipForStokovPaelsWing(
        CardReward reward,
        List<CardRewardAlternative> alternatives)
    {
        if (!ShouldDropSkipForStokovPaelsWing(reward, alternatives))
            return false;

        var skipIndex = alternatives.FindIndex(static option =>
            string.Equals(option.OptionId, "Skip", StringComparison.Ordinal));
        if (skipIndex < 0)
            return false;

        var optionIdsBefore = string.Join(",", alternatives.Select(static option => option.OptionId));
        alternatives.RemoveAt(skipIndex);
        var optionIdsAfter = string.Join(",", alternatives.Select(static option => option.OptionId));

        MainFile.Logger.Info(
            "[StokovPaelsWingCardRewardCompatHelper] stokov-paelswing fallback removed Skip from card reward alternatives"
            + $" | playerNetId={reward.Player.NetId}"
            + $" | rewardSource={ResolveRewardSource(reward)}"
            + $" | optionIdsBefore={optionIdsBefore}"
            + $" | optionIdsAfter={optionIdsAfter}");
        return true;
    }

    private static string ResolveRewardSource(CardReward reward)
    {
        try
        {
            return GetOptions?.Invoke(reward).Source.ToString() ?? "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    private static Func<CardReward, CardCreationOptions>? ResolveOptionsGetter()
    {
        var getter = AccessTools.DeclaredPropertyGetter(typeof(CardReward), "Options");
        if (getter == null)
            return null;

        return AccessTools.MethodDelegate<Func<CardReward, CardCreationOptions>>(getter);
    }
}
