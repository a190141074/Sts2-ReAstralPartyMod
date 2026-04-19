using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonProprietress : AstralPartyRelicModel
{
    private const int MaxCounter = 3;
    private const int ShopGoldGainStep = 10;
    private const int DiscountChancePerShopPercent = 3;
    private const decimal DiscountedPriceMultiplier = 0.3m;

    private static readonly ConditionalWeakTable<MerchantEntry, DiscountRollState> DiscountRollCache =
        new();

    private int _counter = 1;
    private bool _pendingCombatStartTrigger;
    private int _visitedShops;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartTrigger;
    private bool _hasCanonicalVisitedShops;

    [SavedProperty]
    public int AstralParty_PersonProprietressCounter
    {
        get => _counter;
        set
        {
            _counter = value;
            _hasCanonicalCounter = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonProprietressPendingCombatStartTrigger
    {
        get => _pendingCombatStartTrigger;
        set
        {
            _pendingCombatStartTrigger = value;
            _hasCanonicalPendingCombatStartTrigger = true;
        }
    }

    [SavedProperty]
    public int AstralParty_PersonProprietressVisitedShops
    {
        get => _visitedShops;
        set
        {
            _visitedShops = value;
            _hasCanonicalVisitedShops = true;
        }
    }

    // Preserve legacy wire/save names so older Proprietress runs still hydrate correctly.
    public int CurrentDamage
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool IncreasedDamage
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartTrigger && value)
                _pendingCombatStartTrigger = true;
        }
    }

    public int CharacterModel
    {
        get => default;
        set
        {
            if (!_hasCanonicalVisitedShops && value != default)
                _visitedShops = value;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTransfer>()
    ];

    // Keep the combat display aligned with the actual cooldown progress.
    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonProprietressCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonProprietressPendingCombatStartTrigger = true;
        AstralParty_PersonProprietressVisitedShops = 0;
        InvokeDisplayAmountChanged();

        if (Owner.GetRelic<PersonalityDerivativeProprietressWealthism>() == null)
            await RelicCmd.Obtain(
                ModelDb.Relic<PersonalityDerivativeProprietressWealthism>().ToMutable(),
                Owner
            );
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner == null || room.RoomType != RoomType.Shop)
            return;

        AstralParty_PersonProprietressVisitedShops++;
        var goldToGain = ShopGoldGainStep;
        Flash();
        await PlayerCmd.GainGold(goldToGain, Owner);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonProprietressPendingCombatStartTrigger)
        {
            await GrantTransfer();
            AstralParty_PersonProprietressPendingCombatStartTrigger = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < MaxCounter)
            return;

        await GrantTransfer();
        AstralParty_PersonProprietressCounter = 1;
        AstralParty_PersonProprietressPendingCombatStartTrigger = false;
        InvokeDisplayAmountChanged();
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

        var discountController = GetDiscountController(Owner.RunState);
        if (discountController?.Owner != Owner)
            return originalPrice;

        var chancePercent = GetDiscountChancePercent(discountController);
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

    private static int GetDiscountChancePercent(PersonProprietress? controller)
    {
        if (controller == null)
            return 0;

        return Math.Clamp(
            controller.AstralParty_PersonProprietressVisitedShops * DiscountChancePerShopPercent,
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

    private static PersonProprietress? GetDiscountController(IRunState runState)
    {
        return runState.Players
            .Select(player => new
            {
                Player = player,
                Relic = player.GetRelic<PersonProprietress>()
            })
            .Where(entry => entry.Relic != null)
            .OrderByDescending(entry => entry.Relic!.AstralParty_PersonProprietressVisitedShops)
            .ThenBy(entry => entry.Player.NetId)
            .Select(entry => entry.Relic)
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

    private async Task GrantTransfer()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTransfer>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    private sealed class DiscountRollState
    {
        public int ChancePercent { get; set; } = -1;

        public ulong PlayerNetId { get; set; }

        public string EntryKey { get; set; } = string.Empty;

        public bool IsDiscounted { get; set; }
    }
}