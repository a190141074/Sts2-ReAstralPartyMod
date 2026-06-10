using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropLongstandingSolitude : MoonPropStackableRelicBase,
    IRelicExtraIconAmountLabelsProvider,
    IRelicExtraIconAmountLabelsChangeSource
{
    private const decimal PriceIncreasePerStack = 0.25m;

    [SavedProperty] public int AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount { get; set; }
    [SavedProperty] public int AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("FreePurchasesPerFloor", GetFreePurchasesPerFloorText()),
        new StringVar("AvailableFreePurchases", GetAvailableFreePurchases().ToString()),
        new StringVar("PriceIncreasePercent", GetPriceIncreasePercentText()),
        new StringVar("DiscountPercent", GetDiscountPercentText())
    ];

    public event Action? RelicExtraIconAmountLabelsInvalidated;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (IsMelted)
            return;

        if (AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor < 0)
            AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor = Owner?.RunState?.TotalFloor ?? -1;

        RefreshDynamicState();
        NotifyExtraBadgeChanged();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? -1;
        if (totalFloor < 0)
            return Task.CompletedTask;

        if (AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor < 0)
        {
            AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor = totalFloor;
            return Task.CompletedTask;
        }

        var climbedFloors = Math.Max(0, totalFloor - AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor);
        AstralParty_MoonPropLongstandingSolitudeLastProcessedTotalFloor = totalFloor;
        if (climbedFloors <= 0)
            return Task.CompletedTask;

        AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount = Math.Max(
            0,
            AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount + climbedFloors * GetStacks());
        Flash();
        RefreshDynamicState();
        NotifyExtraBadgeChanged();
        return Task.CompletedTask;
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != Owner || !MoonPropLongstandingSolitudeShopHelper.IsEligibleMerchantRoom(player))
            return originalPrice;

        if (MoonPropLongstandingSolitudeShopHelper.ShouldTreatEntryAsFree(player, entry))
            return 0m;
        if (MoonPropLongstandingSolitudeShopHelper.ShouldTreatEntryAsHalfPrice(player, entry))
            return originalPrice * (1m - GetDiscountRatio());
        if (MoonPropLongstandingSolitudeShopHelper.IsMoonPropRelicEntry(entry)
            && MoonPropLongstandingSolitudeShopHelper.HasActiveDiscountCount(player))
            return originalPrice;

        return originalPrice * (1m + PriceIncreasePerStack * GetStacks());
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
            return false;

        return rewards.RemoveAll(static reward => reward is GoldReward) > 0;
    }

    public int GetAvailableFreePurchases()
    {
        return Math.Max(0, AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount);
    }

    public void ConsumeFreePurchase()
    {
        if (AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount <= 0)
            return;

        AstralParty_MoonPropLongstandingSolitudeFreePurchaseCount--;
        RefreshDynamicState();
        NotifyExtraBadgeChanged();
    }

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetRelicExtraIconAmountLabelSlots()
    {
        var count = GetAvailableFreePurchases();
        if (count <= 0)
            return [];

        return
        [
            new ExtraIconAmountLabelSlot(
                count.ToString(),
                ExtraIconAmountLabelCorner.TopLeft,
                default,
                null,
                null)
        ];
    }

    protected override Task AfterStacksChangedAsync()
    {
        RefreshDynamicState();
        NotifyExtraBadgeChanged();
        return Task.CompletedTask;
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("FreePurchasesPerFloor", GetFreePurchasesPerFloorText());
        SetDynamicString("AvailableFreePurchases", GetAvailableFreePurchases().ToString());
        SetDynamicString("PriceIncreasePercent", GetPriceIncreasePercentText());
        SetDynamicString("DiscountPercent", GetDiscountPercentText());
    }

    private string GetFreePurchasesPerFloorText()
    {
        return GetStacks().ToString();
    }

    private string GetPriceIncreasePercentText()
    {
        return FormatPercent(PriceIncreasePerStack * GetStacks());
    }

    private decimal GetDiscountRatio()
    {
        return MoonPropFormulaHelper.GetHalfDecayAccumulatedRatio(GetStacks());
    }

    private string GetDiscountPercentText()
    {
        return FormatPercent(GetDiscountRatio());
    }

    private void NotifyExtraBadgeChanged()
    {
        InvokeDisplayAmountChanged();
        RelicExtraIconAmountLabelsInvalidated?.Invoke();
    }
}
