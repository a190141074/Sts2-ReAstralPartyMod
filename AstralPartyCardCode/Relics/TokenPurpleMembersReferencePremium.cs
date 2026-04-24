using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenPurpleMembersReferencePremium : TokenMembersReferenceRelicBase
{
    protected override int CardsToDraw => 2;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}