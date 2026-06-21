using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class RewardSyncHelper
{
    public static Task<RelicModel> ObtainRelicAsReward(Player owner, RelicModel relic)
    {
        ExclusiveRelicUnlockHelper.MarkRelicUnlockedForCurrentRunAndProfile(owner, relic);
        return PersonMultiplayerEffectHelper.ObtainRelicAsReward(owner, relic);
    }

    public static Task<RelicModel> ObtainRelicAsRewardMultiplayerSafe(Player owner, RelicModel relic)
    {
        ExclusiveRelicUnlockHelper.MarkRelicUnlockedForCurrentRunAndProfile(owner, relic);
        return PersonMultiplayerEffectHelper.ObtainRelicForMultiplayerSafeReward(owner, relic);
    }
}
