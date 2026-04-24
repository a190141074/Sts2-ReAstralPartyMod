using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueDie8 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 8;

    protected override int StarLightRewardAmount => 8;

    protected override int DamageToAllEnemiesAmount => 5;
}