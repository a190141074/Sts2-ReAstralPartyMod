using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class TokenBlueDie10 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 10;

    protected override int StarLightRewardAmount => 10;

    protected override int BlockRewardAmount => 7;
}
