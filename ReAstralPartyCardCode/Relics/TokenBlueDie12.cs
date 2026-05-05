using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(DiceSeriesRelicPool))]
public class TokenBlueDie12 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 12;

    protected override int StarLightRewardAmount => 12;

    protected override int HealRewardAmount => 9;
}
