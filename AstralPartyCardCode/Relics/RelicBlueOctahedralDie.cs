using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class RelicBlueOctahedralDie : AstralPartyRelicModel
{
    private const int TriggerRoundMultiple = 8;
    private const int StarLightRewardAmount = 8;

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        var roundNumber = room.CombatState.RoundNumber;
        if (roundNumber <= 0 || roundNumber % TriggerRoundMultiple != 0)
            return Task.CompletedTask;

        Flash();

        // StarLight only converts into an end-of-combat reward, so grant the
        // equivalent extra reward directly here to avoid hook-order desyncs.
        room.AddExtraReward(Owner, new GoldReward(StarLightRewardAmount, Owner, false));
        return Task.CompletedTask;
    }
}