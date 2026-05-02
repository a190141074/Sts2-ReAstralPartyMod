using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueDie4 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 4;

    protected override int StarLightRewardAmount => 4;

    protected override int EnergyRewardAmount => 2;
}