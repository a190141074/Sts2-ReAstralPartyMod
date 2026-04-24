using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueDie6 : TokenBlueDieRelicBase
{
    protected override int TriggerRoundMultiple => 6;

    protected override int StarLightRewardAmount => 6;

    protected override int CardsToDrawAmount => 3;
}