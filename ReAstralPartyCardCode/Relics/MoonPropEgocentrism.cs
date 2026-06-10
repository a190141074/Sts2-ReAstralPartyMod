using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropEgocentrism : MoonPropStackableRelicBase
{
    private const decimal BaseFloorThreshold = 7m;
    private const decimal ThresholdDecayPerExtraStack = 0.5m;
    private const int BaseAmpouleCap = 3;
    private const int TransformInterval = 20;

    [SavedProperty] public int AstralParty_MoonPropEgocentrismGrantedPotionSlotStacks { get; set; }
    [SavedProperty] public int AstralParty_MoonPropEgocentrismAccumulatedFloors { get; set; }
    [SavedProperty] public int AstralParty_MoonPropEgocentrismLastProcessedTotalFloor { get; set; } = -1;
    [SavedProperty] public int AstralParty_MoonPropEgocentrismLastProcessedRound { get; set; }

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("PotionSlotBonus", GetPotionSlotBonusText()),
        new StringVar("FloorThreshold", GetFloorThresholdText()),
        new StringVar("AmpouleCap", GetAmpouleCapText())
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        HoverTipFactory.FromPotion<ExplosiveAmpoule>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (IsMelted || Owner == null)
            return;

        InitializeFloorAnchor();
        await GrantMissingPotionSlotsAsync();
        RefreshDynamicState();
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? -1;
        if (totalFloor < 0)
            return Task.CompletedTask;

        if (AstralParty_MoonPropEgocentrismLastProcessedTotalFloor < 0)
        {
            AstralParty_MoonPropEgocentrismLastProcessedTotalFloor = totalFloor;
            return Task.CompletedTask;
        }

        var climbedFloors = Math.Max(0, totalFloor - AstralParty_MoonPropEgocentrismLastProcessedTotalFloor);
        AstralParty_MoonPropEgocentrismLastProcessedTotalFloor = totalFloor;
        if (climbedFloors <= 0)
            return Task.CompletedTask;

        AstralParty_MoonPropEgocentrismAccumulatedFloors += climbedFloors;
        return ResolveAmpouleProgressAsync();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var roundNumber = Owner.Creature.CombatState.RoundNumber;
        if (roundNumber <= 0 || AstralParty_MoonPropEgocentrismLastProcessedRound == roundNumber)
            return;

        AstralParty_MoonPropEgocentrismLastProcessedRound = roundNumber;
        if (roundNumber % TransformInterval != 0)
            return;

        var targetRelic = SelectTransformTarget();
        if (targetRelic == null)
            return;

        Flash();
        await RelicCmd.Remove(targetRelic);
        AddStacks(1);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_MoonPropEgocentrismLastProcessedRound = 0;
        return Task.CompletedTask;
    }

    protected override async Task AfterStacksChangedAsync()
    {
        await GrantMissingPotionSlotsAsync();
        await ResolveAmpouleProgressAsync();
        RefreshDynamicState();
    }

    private int GetCurrentFloorThreshold()
    {
        var threshold = BaseFloorThreshold * MoonPropFormulaHelper.GetRepeatedMultiplier(
            ThresholdDecayPerExtraStack,
            Math.Max(GetStacks() - 1, 0));
        return Math.Max(1, (int)decimal.Round(threshold, 0, MidpointRounding.AwayFromZero));
    }

    private int GetAmpouleCap()
    {
        return BaseAmpouleCap + Math.Max(0, GetStacks() - 1);
    }

    private async Task GrantMissingPotionSlotsAsync()
    {
        if (Owner == null)
            return;

        var missingStacks = Math.Max(0, GetStacks() - AstralParty_MoonPropEgocentrismGrantedPotionSlotStacks);
        if (missingStacks <= 0)
            return;

        AstralParty_MoonPropEgocentrismGrantedPotionSlotStacks = GetStacks();
        await PlayerCmd.GainMaxPotionCount(missingStacks, Owner);
    }

    private async Task ResolveAmpouleProgressAsync()
    {
        if (Owner == null)
            return;

        var threshold = GetCurrentFloorThreshold();
        if (threshold <= 0)
            return;

        var gained = false;
        while (AstralParty_MoonPropEgocentrismAccumulatedFloors >= threshold)
        {
            AstralParty_MoonPropEgocentrismAccumulatedFloors -= threshold;
            if (CountOwnedExplosiveAmpoules(Owner) >= GetAmpouleCap())
                continue;

            var result = await PotionCmd.TryToProcure(ModelDb.Potion<ExplosiveAmpoule>().ToMutable(), Owner);
            gained |= result.success;
        }

        if (gained)
            Flash();

        RefreshDynamicState();
    }

    private RelicModel? SelectTransformTarget()
    {
        if (Owner == null)
            return null;

        var eligibleRelics = Owner.Relics
            .Where(relic => !ReferenceEquals(relic, this))
            .Where(relic => !relic.IsMelted)
            .Where(relic => relic is not MoonPropStackableRelicBase)
            .Where(static relic => GetTransformRarityRank(relic.Rarity) < int.MaxValue)
            .ToList();
        if (eligibleRelics.Count == 0)
            return null;

        var lowestRank = eligibleRelics.Min(static relic => GetTransformRarityRank(relic.Rarity));
        var lowestCandidates = eligibleRelics
            .Where(relic => GetTransformRarityRank(relic.Rarity) == lowestRank)
            .OrderBy(static relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry)
            .ThenBy(static relic => relic.Id.Entry)
            .ToList();
        if (lowestCandidates.Count == 0)
            return null;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            lowestCandidates.Count,
            MainFile.ModId,
            RelicId,
            nameof(SelectTransformTarget),
            Owner.RunState?.Rng.StringSeed ?? "<null_seed>",
            Owner.RunState?.CurrentActIndex ?? -1,
            Owner.RunState?.TotalFloor ?? -1,
            Owner.NetId,
            Owner.Creature?.CombatState?.RoundNumber ?? 0,
            lowestRank);
        return lowestCandidates[selectedIndex];
    }

    private void InitializeFloorAnchor()
    {
        if (AstralParty_MoonPropEgocentrismLastProcessedTotalFloor >= 0)
            return;

        AstralParty_MoonPropEgocentrismLastProcessedTotalFloor = Owner?.RunState?.TotalFloor ?? -1;
    }

    private static int CountOwnedExplosiveAmpoules(Player player)
    {
        return player.Potions.Count(static potion =>
            potion.CanonicalInstance is ExplosiveAmpoule || potion is ExplosiveAmpoule);
    }

    private static int GetTransformRarityRank(RelicRarity rarity)
    {
        return rarity switch
        {
            RelicRarity.Common => 0,
            RelicRarity.Uncommon => 1,
            RelicRarity.Rare => 2,
            _ => int.MaxValue
        };
    }

    private string GetPotionSlotBonusText()
    {
        return GetStacks().ToString();
    }

    private string GetFloorThresholdText()
    {
        return GetCurrentFloorThreshold().ToString();
    }

    private string GetAmpouleCapText()
    {
        return GetAmpouleCap().ToString();
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("PotionSlotBonus", GetPotionSlotBonusText());
        SetDynamicString("FloorThreshold", GetFloorThresholdText());
        SetDynamicString("AmpouleCap", GetAmpouleCapText());
    }
}
