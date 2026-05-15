using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleMembersReferencePremium : TokenMembersReferenceRelicBase
{
    protected override int CardsToDraw => 2;

    public override RelicRarity Rarity => RelicRarity.Uncommon;
}
