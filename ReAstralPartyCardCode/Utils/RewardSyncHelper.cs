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
        return PersonaMultiplayerEffectHelper.ObtainRelicAsReward(owner, relic);
    }
}
