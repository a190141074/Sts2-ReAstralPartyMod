using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueDie12 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 12;

    protected override int StarLightRewardAmount => 12;

    protected override int HealRewardAmount => 9;
}
