using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class TokenBlueDie20 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 20;

    protected override int StarLightRewardAmount => 20;

    protected override bool GainOtherDiceCombatStartEffects => true;
}
