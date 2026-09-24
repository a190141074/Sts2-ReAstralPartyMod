using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenPurpleMembersReferencePremium : TokenMembersReferenceRelicBase
{
    protected override int CardsToDraw => 1;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        return player == Owner ? originalPrice * 0.75m : originalPrice;
    }
}
