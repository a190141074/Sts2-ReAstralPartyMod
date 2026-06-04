using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisAvariceScroll : AstralPartyRelicModel
{
    private const int UniqueMaterialDropBonusPermille = 225;
    private const decimal MerchantPriceMultiplier = 0.65m;

    protected override string RelicId => "enigmatic_synthesis_avarice_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        MerchantUiRefreshHelper.TryRefreshCurrentMerchantUi(Owner?.RunState);
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != Owner)
            return originalPrice;
        if (Owner?.RunState is not { CurrentRoom: MerchantRoom })
            return originalPrice;

        return originalPrice * MerchantPriceMultiplier;
    }

    public static int GetUniqueMaterialDropBonusPermille(Player? owner)
    {
        return owner?.GetRelic<EnigmaticSynthesisAvariceScroll>() != null
            ? UniqueMaterialDropBonusPermille
            : 0;
    }

    public static bool PreventsShopEntryDamage(Player? owner)
    {
        return owner?.GetRelic<EnigmaticSynthesisAvariceScroll>() != null;
    }
}
