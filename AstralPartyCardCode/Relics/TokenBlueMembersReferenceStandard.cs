using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenBlueMembersReferenceStandard : TokenMembersReferenceRelicBase
{
    protected override int CardsToDraw => 1;

    public override RelicRarity Rarity => RelicRarity.Common;
}
