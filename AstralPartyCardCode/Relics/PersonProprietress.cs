using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class PersonProprietress : AstralPartyRelicModel
{
    private const int MaxCounter = 4;
    private const int ShopGoldGainStep = 10;
    private const int DiscountChancePerShopPercent = 3;
    private const decimal DiscountedPriceMultiplier = 0.3m;

    private static readonly ConditionalWeakTable<MerchantEntry, DiscountRollState> DiscountRollCache =
        new();

    [SavedProperty] public int AstralParty_PersonProprietressCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonProprietressPendingCombatStartTrigger { get; set; }

    [SavedProperty] public int AstralParty_PersonProprietressVisitedShops { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    // Keep the combat display aligned with the actual cooldown progress.
    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonProprietressCounter = 1;
        AstralParty_PersonProprietressPendingCombatStartTrigger = false;
        AstralParty_PersonProprietressVisitedShops = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null || !AstralParty_PersonProprietressPendingCombatStartTrigger)
            return;

        Flash();

        // TODO: Trigger PersonProprietress's combat effect immediately when a queued cooldown completes between combats.

        AstralParty_PersonProprietressPendingCombatStartTrigger = false;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null || room.RoomType != RoomType.Shop)
            return;

        AstralParty_PersonProprietressVisitedShops++;
        var goldToGain = AstralParty_PersonProprietressVisitedShops * ShopGoldGainStep;
        Flash();
        await PlayerCmd.GainGold(goldToGain, Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (GetClampedCounter() < MaxCounter)
            return;

        Flash();

        // TODO: Implement PersonProprietress's combat effect when the 3-turn cooldown completes.

        AstralParty_PersonProprietressCounter = 1;
        AstralParty_PersonProprietressPendingCombatStartTrigger = false;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (Owner?.RunState is not { CurrentRoom: MerchantRoom })
            return originalPrice;

        if (GetDiscountController(Owner.RunState) != Owner)
            return originalPrice;

        var chancePercent = GetDiscountChancePercent();
        if (chancePercent <= 0)
            return originalPrice;

        if (!ShouldDiscountEntry(player, entry, chancePercent))
            return originalPrice;

        return originalPrice * DiscountedPriceMultiplier;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonProprietressCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonProprietressCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonProprietressPendingCombatStartTrigger)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonProprietressCounter = 1;
            AstralParty_PersonProprietressPendingCombatStartTrigger = true;
            return;
        }

        AdvanceCounter();
    }

    private int GetDiscountChancePercent()
    {
        return Math.Clamp(
            AstralParty_PersonProprietressVisitedShops * DiscountChancePerShopPercent,
            0,
            100
        );
    }

    private bool ShouldDiscountEntry(Player player, MerchantEntry entry, int chancePercent)
    {
        var state = DiscountRollCache.GetOrCreateValue(entry);
        var entryKey = BuildMerchantEntryKey(entry);
        if (
            state.ChancePercent == chancePercent
            && state.PlayerNetId == player.NetId
            && state.EntryKey == entryKey
        )
            return state.IsDiscounted;

        var rollKey =
            $"{Owner!.RunState.Rng.StringSeed}|{chancePercent}|{player.NetId}|{entryKey}";
        var roll = (int)((uint)StringHelper.GetDeterministicHashCode(rollKey) % 100u);

        state.ChancePercent = chancePercent;
        state.PlayerNetId = player.NetId;
        state.EntryKey = entryKey;
        state.IsDiscounted = roll < chancePercent;
        return state.IsDiscounted;
    }

    private static Player? GetDiscountController(IRunState runState)
    {
        return runState
            .Players.Where(player => player.GetRelic<PersonProprietress>() != null)
            .OrderBy(player => player.NetId)
            .FirstOrDefault();
    }

    private static string BuildMerchantEntryKey(MerchantEntry entry)
    {
        return entry switch
        {
            MerchantCardEntry cardEntry when cardEntry.CreationResult != null =>
                $"card:{cardEntry.CreationResult.Card.Id}",
            MerchantRelicEntry relicEntry when relicEntry.Model != null =>
                $"relic:{relicEntry.Model.Id}",
            MerchantPotionEntry potionEntry when potionEntry.Model != null =>
                $"potion:{potionEntry.Model.Id}",
            // Treat card removal as a merchant entry too so the discount can apply to the service.
            MerchantCardRemovalEntry => "card_removal",
            _ => entry.GetType().FullName ?? entry.GetType().Name
        };
    }

    private sealed class DiscountRollState
    {
        public int ChancePercent { get; set; } = -1;

        public ulong PlayerNetId { get; set; }

        public string EntryKey { get; set; } = string.Empty;

        public bool IsDiscounted { get; set; }
    }
}