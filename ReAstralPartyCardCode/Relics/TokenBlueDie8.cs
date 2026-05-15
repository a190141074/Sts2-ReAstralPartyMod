using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueDie8 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 8;

    protected override int StarLightRewardAmount => 8;

    protected override int DamageToAllEnemiesAmount => 5;
}
