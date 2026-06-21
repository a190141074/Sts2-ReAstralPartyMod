using System.Text.Json;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs.History;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class ProphecySoulDevour : AstralPartyRelicModel
{
    private readonly List<int> _pendingNodeDiscoveries = [];
    private readonly List<ProphecySoulDevourDelayedCardGrant> _pendingDelayedCards = [];
    private readonly List<ProphecySoulDevourDelayedRelicGrant> _pendingDelayedRelics = [];
    private readonly List<ProphecySoulDevourDelayedCardGrant> _readyDelayedCards = [];
    private readonly List<ProphecySoulDevourDelayedRelicGrant> _readyDelayedRelics = [];
    private readonly List<ProphecySoulDevourMimicSnapshot> _mimicSnapshots = [];

    [SavedProperty] public int AstralParty_ProphecySoulDevourDiscoveryBattleCounter { get; set; } = 3;
    [SavedProperty] public int AstralParty_ProphecySoulDevourPendingDiscoveryCounter { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourDiscoverySequenceCounter { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourLastProcessedFloorCounter { get; set; } = -1;
    [SavedProperty] public bool AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourEventStopPending { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourDisableNextSmithPending { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourPendingSmithBonusCounter { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourTreasureHuntingPermanent { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourMatrixRecyclingPermanent { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourAncientRuinsPermanent { get; set; }
    [SavedProperty] public bool AstralParty_ProphecySoulDevourMineralRecoveryPermanent { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourEnergySavingStrategyStacks { get; set; }
    [SavedProperty] public int AstralParty_ProphecySoulDevourHiddenRelicSelectionSequenceCounter { get; set; }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourPendingNodeDiscoveriesJson
    {
        get => JsonSerializer.Serialize(_pendingNodeDiscoveries.ToArray());
        set
        {
            _pendingNodeDiscoveries.Clear();
            try
            {
                _pendingNodeDiscoveries.AddRange(JsonSerializer.Deserialize<int[]>(value) ?? []);
            }
            catch
            {
            }
        }
    }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourPendingDelayedCardsJson
    {
        get => ProphecySoulDevourHelper.SerializeDelayedCards(_pendingDelayedCards);
        set
        {
            _pendingDelayedCards.Clear();
            _pendingDelayedCards.AddRange(ProphecySoulDevourHelper.DeserializeDelayedCards(value));
        }
    }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourPendingDelayedRelicsJson
    {
        get => ProphecySoulDevourHelper.SerializeDelayedRelics(_pendingDelayedRelics);
        set
        {
            _pendingDelayedRelics.Clear();
            _pendingDelayedRelics.AddRange(ProphecySoulDevourHelper.DeserializeDelayedRelics(value));
        }
    }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourReadyDelayedCardsJson
    {
        get => ProphecySoulDevourHelper.SerializeDelayedCards(_readyDelayedCards);
        set
        {
            _readyDelayedCards.Clear();
            _readyDelayedCards.AddRange(ProphecySoulDevourHelper.DeserializeDelayedCards(value));
        }
    }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourReadyDelayedRelicsJson
    {
        get => ProphecySoulDevourHelper.SerializeDelayedRelics(_readyDelayedRelics);
        set
        {
            _readyDelayedRelics.Clear();
            _readyDelayedRelics.AddRange(ProphecySoulDevourHelper.DeserializeDelayedRelics(value));
        }
    }

    [SavedProperty]
    private string AstralParty_ProphecySoulDevourMimicSnapshotsJson
    {
        get => ProphecySoulDevourHelper.SerializeMimicSnapshots(_mimicSnapshots);
        set
        {
            _mimicSnapshots.Clear();
            _mimicSnapshots.AddRange(ProphecySoulDevourHelper.DeserializeMimicSnapshots(value));
        }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;
    public override bool ShowCounter => true;
    public override int DisplayAmount => AstralParty_ProphecySoulDevourPendingDiscoveryCounter > 0
        ? 0
        : Math.Max(0, AstralParty_ProphecySoulDevourDiscoveryBattleCounter);

    internal List<int> PendingNodeDiscoveries => _pendingNodeDiscoveries;
    internal List<ProphecySoulDevourDelayedCardGrant> PendingDelayedCards => _pendingDelayedCards;
    internal List<ProphecySoulDevourDelayedRelicGrant> PendingDelayedRelics => _pendingDelayedRelics;
    internal List<ProphecySoulDevourDelayedCardGrant> ReadyDelayedCards => _readyDelayedCards;
    internal List<ProphecySoulDevourDelayedRelicGrant> ReadyDelayedRelics => _readyDelayedRelics;
    internal List<ProphecySoulDevourMimicSnapshot> MimicSnapshots => _mimicSnapshots;

    internal void NotifyDisplayAmountChanged()
    {
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        ProphecySoulDevourHelper.QueueImmediateDiscovery(this);
        InvokeDisplayAmountChanged();
        await TryResolvePendingDiscoveriesAsync("after_obtained");
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        ProphecySoulDevourHelper.AdvanceAfterCombat(this);
        InvokeDisplayAmountChanged();
        await ResolveReadyDelayedGrantsAsync();
        await TryResolvePendingDiscoveriesAsync("after_combat_end");
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        var totalFloor = Owner?.RunState?.TotalFloor ?? -1;
        if (totalFloor >= 0)
        {
            if (AstralParty_ProphecySoulDevourLastProcessedFloorCounter < 0)
            {
                AstralParty_ProphecySoulDevourLastProcessedFloorCounter = totalFloor;
            }
            else
            {
                var climbedFloors = Math.Max(0, totalFloor - AstralParty_ProphecySoulDevourLastProcessedFloorCounter);
                AstralParty_ProphecySoulDevourLastProcessedFloorCounter = totalFloor;
                ProphecySoulDevourHelper.AdvanceNodeProgress(this, climbedFloors);
                InvokeDisplayAmountChanged();
            }
        }

        if (room is not CombatRoom)
            await ResolveReadyDelayedGrantsAsync();

        await TryResolvePendingDiscoveriesAsync("after_room_entered");
    }

    public override async Task BeforeCombatStart()
    {
        await ResolveMimicLarvaSnapshotsAsync();
        await ResolvePendingEventStopAsync();
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;

        var modified = false;
        if (AstralParty_ProphecySoulDevourDisableNextSmithPending)
        {
            modified |= RemoveSmithOptions(options);
            AstralParty_ProphecySoulDevourDisableNextSmithPending = false;
        }
        else if (!AstralParty_ProphecySoulDevourAncientRuinsPermanent && AstralParty_ProphecySoulDevourPendingSmithBonusCounter > 0)
        {
            var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
            if (smithOption != null)
            {
                smithOption.SmithCount += AstralParty_ProphecySoulDevourPendingSmithBonusCounter;
                modified = true;
            }
        }

        return modified;
    }

    internal bool ShouldInterceptSmithRestSiteOption()
    {
        return AstralParty_ProphecySoulDevourAncientRuinsPermanent && Owner != null;
    }

    public override decimal ModifyMerchantPrice(Player player, MerchantEntry entry, decimal originalPrice)
    {
        if (player != Owner)
            return originalPrice;
        if (AstralParty_ProphecySoulDevourEnergySavingStrategyStacks <= 0)
            return originalPrice;

        return Math.Max(0m, originalPrice - AstralParty_ProphecySoulDevourEnergySavingStrategyStacks * 7m);
    }

    public override bool TryModifyRewardsLate(Player player, List<Reward> rewards, AbstractRoom? room)
    {
        if (player != Owner || room is not CombatRoom)
            return false;

        var modified = false;
        if (AstralParty_ProphecySoulDevourTreasureHuntingPermanent)
        {
            var cardRewardCount = rewards.OfType<CardReward>().Count(reward => reward.Cards.Any());
            var previousProgress = AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter;
            AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter += cardRewardCount;
            while (AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter >= 5)
            {
                AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter -= 5;
                rewards.Add(new CardReward(CardCreationOptions.ForRoom(player, room.RoomType), 3, player));
                modified = true;
            }

            if (previousProgress != AstralParty_ProphecySoulDevourTreasureHuntingProgressCounter)
                modified = true;
        }

        return modified;
    }

    public override bool TryModifyCardRewardAlternatives(
        Player player,
        CardReward cardReward,
        List<CardRewardAlternative> alternatives)
    {
        if (player != Owner)
            return false;
        if (!AstralParty_ProphecySoulDevourMatrixRecyclingPermanent)
            return false;

        var skipIndex = alternatives.FindIndex(static option => string.Equals(option.OptionId, "Skip", StringComparison.Ordinal));
        if (skipIndex < 0)
            return false;

        alternatives[skipIndex] = new CardRewardAlternative(
            "Skip",
            async () =>
            {
                await PersonMultiplayerEffectHelper.GainGoldDeterministic(20m, player);
                var historyEntry = LocalContext.NetId.HasValue
                    ? player.RunState.CurrentMapPointHistoryEntry?.GetEntry(LocalContext.NetId.Value)
                    : null;
                foreach (var card in cardReward.Cards)
                {
                    historyEntry?.CardChoices.Add(new CardChoiceHistoryEntry(card, wasPicked: false));
                    RunManager.Instance?.RewardSynchronizer?.SyncLocalSkippedCard(card);
                }
            },
            PostAlternateCardRewardAction.EndSelectionAndCompleteReward);
        return true;
    }

    public async Task OnRunDeckCardRemovedAsync()
    {
        if (!AstralParty_ProphecySoulDevourMineralRecoveryPermanent || Owner == null)
            return;

        await PersonMultiplayerEffectHelper.GainGoldDeterministic(10m, Owner);
        Flash();
    }

    internal async Task<bool> ResolveAncientRuinsSmithAsync()
    {
        if (!AstralParty_ProphecySoulDevourAncientRuinsPermanent || Owner == null)
            return false;

        var smithCount = Math.Max(1, 1 + AstralParty_ProphecySoulDevourPendingSmithBonusCounter);
        if (smithCount <= 0)
            return false;

        var successCount = 0;
        for (var i = 0; i < smithCount; i++)
        {
            await ProphecySoulDevourHelper.ResolveCardDiscoveryAsync(
                null,
                this,
                CardRarity.Rare,
                "ancient_ruins");
            successCount++;
        }

        if (successCount <= 0)
            return false;

        AstralParty_ProphecySoulDevourPendingSmithBonusCounter = 0;
        Flash();
        return true;
    }

    internal LocString BuildAncientRuinsRestSiteDescription()
    {
        return ProphecySoulDevourRegistry.AncientRuinsRestSiteDescription;
    }

    private async Task TryResolvePendingDiscoveriesAsync(string reason)
    {
        if (Owner == null)
            return;

        while (AstralParty_ProphecySoulDevourPendingDiscoveryCounter > 0)
        {
            var selectionResult = await ProphecySoulDevourHelper.OpenDiscoveryAsync(this);
            if (selectionResult == null)
                break;

            if (selectionResult.RefreshCount > 0)
            {
                var totalCost = selectionResult.RefreshCount * selectionResult.RefreshCost;
                if (totalCost > 0)
                    await PersonMultiplayerEffectHelper.LoseGoldDeterministic(totalCost, Owner, GoldLossType.Spent);
            }

            AstralParty_ProphecySoulDevourPendingDiscoveryCounter--;
            AstralParty_ProphecySoulDevourDiscoverySequenceCounter++;
            var shouldConsumeRefreshFlag = AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending;
            if (shouldConsumeRefreshFlag)
                AstralParty_ProphecySoulDevourRefreshNextDiscoveryPending = false;

            await ProphecySoulDevourHelper.ResolveProphecyAsync(null, this, selectionResult.SelectedProphecy);
            InvokeDisplayAmountChanged();
            MainFile.Logger.Info(
                $"[ProphecySoulDevour] resolved discovery | owner={Owner.NetId} | sequence={AstralParty_ProphecySoulDevourDiscoverySequenceCounter} | reason={reason} | selected={selectionResult.SelectedProphecy} | rerolls={selectionResult.RefreshCount}");
        }
    }

    private async Task ResolveReadyDelayedGrantsAsync()
    {
        if (Owner == null)
            return;

        for (var i = _readyDelayedCards.Count - 1; i >= 0; i--)
        {
            var grant = _readyDelayedCards[i];
            var card = ModelDb.GetById<CardModel>(grant.CardId);
            if (card != null)
            {
                var mutableCard = (card.CanonicalInstance ?? card).ToMutable();
                while (mutableCard.CurrentUpgradeLevel < grant.UpgradeLevel)
                {
                    mutableCard.UpgradeInternal();
                    mutableCard.FinalizeUpgradeInternal();
                }

                mutableCard.FloorAddedToDeck = Math.Max(Owner.RunState?.TotalFloor ?? 1, 1);
                await EventDeckCardHelper.AddCardToRunDeckAsync(Owner, mutableCard);
            }

            _readyDelayedCards.RemoveAt(i);
        }

        for (var i = _readyDelayedRelics.Count - 1; i >= 0; i--)
        {
            var grant = _readyDelayedRelics[i];
            var relic = ModelDb.GetById<RelicModel>(grant.RelicId);
            if (relic != null)
                await PersonMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, relic.CanonicalInstance ?? relic);

            _readyDelayedRelics.RemoveAt(i);
        }
    }

    private async Task ResolveMimicLarvaSnapshotsAsync()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        foreach (var snapshot in _mimicSnapshots)
        {
            var card = ModelDb.GetById<CardModel>(snapshot.CardId);
            if (card == null)
                continue;

            var copy = (card.CanonicalInstance ?? card).ToMutable();
            while (copy.CurrentUpgradeLevel < snapshot.UpgradeLevel)
            {
                copy.UpgradeInternal();
                copy.FinalizeUpgradeInternal();
            }

            if (!copy.Keywords.Contains(CardKeyword.Exhaust))
                CardCmd.ApplyKeyword(copy, CardKeyword.Exhaust);

            copy.Owner = Owner;
            await CardPileCmd.Add(copy, PileType.Draw, CardPilePosition.Bottom, this);
        }
    }

    private async Task ResolvePendingEventStopAsync()
    {
        if (!AstralParty_ProphecySoulDevourEventStopPending || Owner?.Creature?.CombatState == null)
            return;

        var enemies = Owner.Creature.CombatState.Enemies.ToList();
        foreach (var enemy in enemies)
            await CreatureCmd.Stun(enemy);

        AstralParty_ProphecySoulDevourEventStopPending = false;
        Flash();
    }

    private static bool RemoveSmithOptions(ICollection<RestSiteOption> options)
    {
        var smithOptions = options.OfType<SmithRestSiteOption>().ToList();
        if (smithOptions.Count == 0)
            return false;

        foreach (var smithOption in smithOptions)
            options.Remove(smithOption);

        return true;
    }

    internal void OnRestSiteOptionResolved(RestSiteOption option)
    {
        if (option is SmithRestSiteOption)
            AstralParty_ProphecySoulDevourPendingSmithBonusCounter = 0;
    }
}
