using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenBlueDie6 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 6;

    protected override int StarLightRewardAmount => 6;

    protected override int CardsToDrawAmount => 3;
}