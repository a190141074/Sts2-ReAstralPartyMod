using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class MoonPropLongstandingSolitudeShopHelper
{
    public static bool HasActiveDiscountCount(Player? owner)
    {
        return owner?.GetRelic<ReAstralPartyMod.ReAstralPartyCardCode.Relics.MoonPropLongstandingSolitude>() is { } relic
               && IsEligibleMerchantRoom(owner)
               && relic.GetAvailableFreePurchases() > 0;
    }

    public static bool IsFreeEligibleEntry(MerchantEntry? entry)
    {
        return entry is MerchantCardEntry;
    }

    public static bool IsHalfPriceEligibleEntry(MerchantEntry? entry)
    {
        return entry is MerchantPotionEntry
               or MerchantCardRemovalEntry
               or MerchantRelicEntry;
    }

    public static bool IsMoonPropRelicEntry(MerchantEntry? entry)
    {
        return entry is MerchantRelicEntry { Model: ReAstralPartyMod.ReAstralPartyCardCode.Relics.MoonPropStackableRelicBase };
    }

    public static bool IsEligibleMerchantRoom(Player? owner)
    {
        return owner?.RunState?.CurrentRoom is MerchantRoom;
    }

    public static bool ShouldTreatEntryAsFree(Player? owner, MerchantEntry? entry)
    {
        if (!IsFreeEligibleEntry(entry))
            return false;

        return HasActiveDiscountCount(owner);
    }

    public static bool ShouldTreatEntryAsHalfPrice(Player? owner, MerchantEntry? entry)
    {
        if (!HasActiveDiscountCount(owner))
            return false;
        if (IsMoonPropRelicEntry(entry))
            return false;

        return IsHalfPriceEligibleEntry(entry);
    }

    public static bool TryConsumeFreePurchase(Player? owner, MerchantEntry? entry)
    {
        if (IsMoonPropRelicEntry(entry))
            return false;
        if (!IsFreeEligibleEntry(entry) && !ShouldTreatEntryAsHalfPrice(owner, entry))
            return false;
        if (owner?.GetRelic<ReAstralPartyMod.ReAstralPartyCardCode.Relics.MoonPropLongstandingSolitude>() is not { } relic)
            return false;
        if (!IsEligibleMerchantRoom(owner))
            return false;
        if (relic.GetAvailableFreePurchases() <= 0)
            return false;

        relic.ConsumeFreePurchase();
        MerchantUiRefreshHelper.TryRefreshCurrentMerchantUi(owner.RunState);
        return true;
    }

    public static decimal ResolveDisplayedPrice(Player? owner, decimal originalPrice)
    {
        return originalPrice;
    }
}
