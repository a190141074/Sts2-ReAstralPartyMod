using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueDie12 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 12;

    protected override int StarLightRewardAmount => 12;

    protected override int HealRewardAmount => 9;
}