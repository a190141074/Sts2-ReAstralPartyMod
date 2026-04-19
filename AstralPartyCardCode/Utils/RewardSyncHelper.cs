using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class RewardSyncHelper
{
    public static Task<RelicModel> ObtainRelicAsReward(Player owner, RelicModel relic)
    {
        RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedRelic(relic);
        return RelicCmd.Obtain(relic.ToMutable(), owner);
    }
}