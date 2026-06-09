using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropFocusedConvergence : MoonPropStackableRelicBase
{
    private const decimal BaseFloorThreshold = 15m;
    private const decimal ThresholdDecayPerExtraStack = 0.7m;

    [SavedProperty] public int AstralParty_MoonPropFocusedConvergencePendingSmithBonus { get; set; }
    [SavedProperty] public int AstralParty_MoonPropFocusedConvergenceAccumulatedFloors { get; set; }
    [SavedProperty] public int AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor { get; set; } = -1;

    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("FloorThreshold", GetFloorThresholdText()),
        new StringVar("RestHealPenaltyPercent", GetRestHealPenaltyPercentText())
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (IsMelted)
            return;

        InitializeFloorAnchor();
        ResolvePendingSmithProgress();
        RefreshDynamicState();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? -1;
        if (totalFloor < 0)
            return Task.CompletedTask;

        if (AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor < 0)
        {
            AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor = totalFloor;
            return Task.CompletedTask;
        }

        var climbedFloors = Math.Max(0, totalFloor - AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor);
        AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor = totalFloor;
        if (climbedFloors <= 0)
            return Task.CompletedTask;

        AstralParty_MoonPropFocusedConvergenceAccumulatedFloors += climbedFloors;
        ResolvePendingSmithProgress();
        return Task.CompletedTask;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;
        if (AstralParty_MoonPropFocusedConvergencePendingSmithBonus <= 0)
            return false;

        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption == null)
            return false;

        smithOption.SmithCount += AstralParty_MoonPropFocusedConvergencePendingSmithBonus;
        return true;
    }

    public override decimal ModifyRestSiteHealAmount(Creature creature, decimal amount)
    {
        if (Owner?.Creature == null || creature != Owner.Creature || amount <= 0m)
            return amount;

        return amount * GetRestHealMultiplier();
    }

    internal void OnRestSiteOptionResolved(bool usedSmith)
    {
        if (!usedSmith || AstralParty_MoonPropFocusedConvergencePendingSmithBonus <= 0)
            return;

        AstralParty_MoonPropFocusedConvergencePendingSmithBonus = 0;
        Flash();
    }

    protected override Task AfterStacksChangedAsync()
    {
        ResolvePendingSmithProgress();
        RefreshDynamicState();
        return Task.CompletedTask;
    }

    private int GetCurrentFloorThreshold()
    {
        var threshold = BaseFloorThreshold * MoonPropFormulaHelper.GetRepeatedMultiplier(
            ThresholdDecayPerExtraStack,
            Math.Max(GetStacks() - 1, 0));
        return Math.Max(1, (int)decimal.Round(threshold, 0, MidpointRounding.AwayFromZero));
    }

    private decimal GetRestHealMultiplier()
    {
        return MoonPropFormulaHelper.GetHalfDecayRatio(GetStacks());
    }

    private string GetFloorThresholdText()
    {
        return GetCurrentFloorThreshold().ToString();
    }

    private string GetRestHealPenaltyPercentText()
    {
        return FormatPercent(1m - GetRestHealMultiplier());
    }

    private void InitializeFloorAnchor()
    {
        if (AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor >= 0)
            return;

        AstralParty_MoonPropFocusedConvergenceLastProcessedTotalFloor = Owner?.RunState?.TotalFloor ?? -1;
    }

    private void ResolvePendingSmithProgress()
    {
        var threshold = GetCurrentFloorThreshold();
        if (threshold <= 0)
            return;

        var gained = 0;
        while (AstralParty_MoonPropFocusedConvergenceAccumulatedFloors >= threshold)
        {
            AstralParty_MoonPropFocusedConvergenceAccumulatedFloors -= threshold;
            AstralParty_MoonPropFocusedConvergencePendingSmithBonus++;
            gained++;
        }

        if (gained <= 0)
            return;

        Flash();
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("FloorThreshold", GetFloorThresholdText());
        SetDynamicString("RestHealPenaltyPercent", GetRestHealPenaltyPercentText());
    }
}
