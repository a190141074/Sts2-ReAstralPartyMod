using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisAvariceScroll : AstralPartyRelicModel
{
    private const int UniqueMaterialDropBonusPermille = 225;
    private const int EmeraldRewardPermille = 150;
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

    public static Reward? TryCreateBonusEmeraldReward(Player? owner, string sourceTag, params object?[] extraContext)
    {
        if (owner?.GetRelic<EnigmaticSynthesisAvariceScroll>() == null)
            return null;

        var context = new object?[8 + extraContext.Length];
        context[0] = MainFile.ModId;
        context[1] = RingOfSevenCursesHelper.SeriesId;
        context[2] = "enigmatic_synthesis_avarice_scroll";
        context[3] = sourceTag;
        context[4] = owner.RunState?.Rng.StringSeed;
        context[5] = owner.RunState?.CurrentActIndex;
        context[6] = owner.RunState?.TotalFloor;
        context[7] = owner.NetId;
        for (var i = 0; i < extraContext.Length; i++)
            context[8 + i] = extraContext[i];

        if (!RingOfSevenCursesHelper.RollPermille(EmeraldRewardPermille, context))
            return null;

        return EnigmaticRewardRegistry.CreateUniqueMaterialReward(owner, EnigmaticUniqueMaterialKind.Emerald, 1);
    }
}
