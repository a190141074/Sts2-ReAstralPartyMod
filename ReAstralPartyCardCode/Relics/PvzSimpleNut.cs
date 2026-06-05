using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Combat;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class PvzSimpleNut : AstralPartyRelicModel
{
    private const decimal DamageThreshold = 80m;
    private decimal _runDamageTaken;
    private readonly List<string> _pendingRewardRelicIds = [];

    [SavedProperty] public bool AstralParty_PvzSimpleNutExhaustedThisRun { get; set; }
    [SavedProperty] public int AstralParty_PvzSimpleNutResolvedMilestoneCount { get; set; }
    [SavedProperty] public int AstralParty_PvzSimpleNutPendingChoiceCount { get; set; }
    [SavedProperty]
    private string AstralParty_PvzSimpleNutPendingRewardRelicIdsSerialized
    {
        get => string.Join("|", _pendingRewardRelicIds);
        set
        {
            _pendingRewardRelicIds.Clear();
            if (string.IsNullOrWhiteSpace(value))
                return;

            foreach (var id in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _pendingRewardRelicIds.Add(id);
        }
    }
    [SavedProperty]
    private string AstralParty_PvzSimpleNutRunDamageTaken
    {
        get => StableNumericStateHelper.SerializeDecimal(_runDamageTaken);
        set => _runDamageTaken = StableNumericStateHelper.DeserializeDecimal(value);
    }

    public override RelicRarity Rarity => RelicRarity.Common;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => StableNumericStateHelper.FloorToNonNegativeInt(_runDamageTaken);

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PvzSimpleNutExhaustedThisRun = false;
        AstralParty_PvzSimpleNutResolvedMilestoneCount = 0;
        AstralParty_PvzSimpleNutPendingChoiceCount = 0;
        _runDamageTaken = 0m;
        _pendingRewardRelicIds.Clear();
        InvokeDisplayAmountChanged();
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        var ownerCreature = Owner?.Creature;
        if (ownerCreature == null)
            return Task.CompletedTask;
        if (AstralParty_PvzSimpleNutExhaustedThisRun)
            return Task.CompletedTask;
        if (!PvzNutRelicHelper.IsOwnedByTarget(target, ownerCreature))
            return Task.CompletedTask;
        if (result.UnblockedDamage <= 0m)
            return Task.CompletedTask;

        var beforeDamageTaken = _runDamageTaken;
        var beforePendingChoiceCount = AstralParty_PvzSimpleNutPendingChoiceCount;
        var beforeResolvedMilestoneCount = AstralParty_PvzSimpleNutResolvedMilestoneCount;
        var beforePendingRewardCount = _pendingRewardRelicIds.Count;

        _runDamageTaken += result.UnblockedDamage;
        var totalTriggeredMilestones = StableNumericStateHelper.FloorDivisionToNonNegativeInt(_runDamageTaken, DamageThreshold);
        var accountedMilestones = AstralParty_PvzSimpleNutResolvedMilestoneCount + AstralParty_PvzSimpleNutPendingChoiceCount +
                                  _pendingRewardRelicIds.Count;
        var newlyPendingChoices = Math.Max(0, totalTriggeredMilestones - accountedMilestones);
        if (newlyPendingChoices > 0)
            AstralParty_PvzSimpleNutPendingChoiceCount += newlyPendingChoices;

        InvokeDisplayAmountChanged();
        MainFile.Logger.Info(
            $"[PvzSimpleNut] Damage accumulated | owner={Owner?.NetId} | targetType={target.GetType().Name} | dealerType={dealer?.GetType().Name ?? "<none>"} | unblocked={result.UnblockedDamage} | beforeDamage={beforeDamageTaken} | afterDamage={_runDamageTaken} | resolvedBefore={beforeResolvedMilestoneCount} | pendingBefore={beforePendingChoiceCount} | pendingAfter={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewardsBefore={beforePendingRewardCount} | pendingRewardsAfter={_pendingRewardRelicIds.Count} | nextThreshold={GetNextMilestoneThreshold()}");
        if (newlyPendingChoices > 0)
        {
            Flash();
            MainFile.Logger.Info(
                $"[PvzSimpleNut] Milestones queued | owner={Owner?.NetId} | damage={_runDamageTaken} | newlyPending={newlyPendingChoices} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount}");
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == null || player != Owner)
            return;

        MainFile.Logger.Info(
            $"[PvzSimpleNut] Player turn start | owner={Owner.NetId} | damage={_runDamageTaken} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count} | exhausted={AstralParty_PvzSimpleNutExhaustedThisRun}");
        await TryResolvePendingChoicesAsync(choiceContext, "after_player_turn_start");
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (cardPlay.Card.Owner != Owner)
            return;

        await TryResolvePendingChoicesAsync(choiceContext, "after_card_played");
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (Owner.Creature.Side != side)
            return;

        await TryResolvePendingChoicesAsync(choiceContext, "after_turn_end");
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        MainFile.Logger.Info(
            $"[PvzSimpleNut] After combat end | owner={Owner.NetId} | damage={_runDamageTaken} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count} | exhausted={AstralParty_PvzSimpleNutExhaustedThisRun}");

        while (!AstralParty_PvzSimpleNutExhaustedThisRun &&
               TryDequeuePendingRewardRelic(out var pendingReward) &&
               pendingReward != null)
        {
            if (PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(Owner, pendingReward))
            {
                break;
            }

            room.AddExtraReward(Owner, new RelicReward(pendingReward.ToMutable(), Owner));
            AstralParty_PvzSimpleNutResolvedMilestoneCount += 1;
            MainFile.Logger.Info($"[PvzSimpleNut] Added selected rare nut to combat reward | owner={Owner.NetId} | reward={pendingReward.Id.Entry} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count}");

            var rewardOptions = PvzNutRelicHelper.GetAvailableRareNutChoices(Owner);
            if (rewardOptions.Count == 0)
            {
                AstralParty_PvzSimpleNutExhaustedThisRun = true;
                MainFile.Logger.Info($"[PvzSimpleNut] All rare nuts obtained; relic exhausted | owner={Owner.NetId} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount}");
            }
        }

        return Task.CompletedTask;
    }

    private decimal GetNextMilestoneThreshold()
    {
        return PvzNutRelicHelper.GetNextRunDamageThreshold(
            AstralParty_PvzSimpleNutResolvedMilestoneCount + AstralParty_PvzSimpleNutPendingChoiceCount + _pendingRewardRelicIds.Count,
            DamageThreshold);
    }

    private async Task TryResolvePendingChoicesAsync(PlayerChoiceContext? choiceContext, string sourceTag)
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        if (AstralParty_PvzSimpleNutExhaustedThisRun)
            return;

        MainFile.Logger.Info(
            $"[PvzSimpleNut] Resolve pending choices | owner={Owner.NetId} | source={sourceTag} | damage={_runDamageTaken} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count} | exhausted={AstralParty_PvzSimpleNutExhaustedThisRun}");

        while (!AstralParty_PvzSimpleNutExhaustedThisRun && AstralParty_PvzSimpleNutPendingChoiceCount > 0)
        {
            var rewardOptions = PvzNutRelicHelper.GetAvailableRareNutChoices(Owner);
            MainFile.Logger.Info(
                $"[PvzSimpleNut] Attempting rare nut selection | owner={Owner.NetId} | source={sourceTag} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count} | candidateCount={rewardOptions.Count} | nextMilestone={AstralParty_PvzSimpleNutResolvedMilestoneCount + _pendingRewardRelicIds.Count + 1}");
            if (rewardOptions.Count == 0)
            {
                AstralParty_PvzSimpleNutExhaustedThisRun = true;
                AstralParty_PvzSimpleNutPendingChoiceCount = 0;
                MainFile.Logger.Info(
                    $"[PvzSimpleNut] No rare nut candidates remain; relic exhausted | owner={Owner.NetId} | source={sourceTag} | damage={_runDamageTaken} | resolvedMilestones={AstralParty_PvzSimpleNutResolvedMilestoneCount}");
                break;
            }

            Flash();
            var selectedRelic = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
                Owner,
                rewardOptions,
                $"{Id.Entry}.rare-nut-choice.{AstralParty_PvzSimpleNutResolvedMilestoneCount + _pendingRewardRelicIds.Count + AstralParty_PvzSimpleNutPendingChoiceCount}");
            if (selectedRelic == null)
            {
                MainFile.Logger.Warn(
                    $"[PvzSimpleNut] Rare nut selection returned null | owner={Owner.NetId} | source={sourceTag} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount}");
                break;
            }

            _pendingRewardRelicIds.Add(selectedRelic.CanonicalInstance.Id.ToString());
            AstralParty_PvzSimpleNutPendingChoiceCount -= 1;
            MainFile.Logger.Info(
                $"[PvzSimpleNut] Selected rare nut reward | owner={Owner.NetId} | source={sourceTag} | reward={selectedRelic.Id.Entry} | pendingChoices={AstralParty_PvzSimpleNutPendingChoiceCount} | pendingRewards={_pendingRewardRelicIds.Count}");
        }
    }

    private bool TryDequeuePendingRewardRelic(out RelicModel? relic)
    {
        relic = null;
        while (_pendingRewardRelicIds.Count > 0)
        {
            var serializedId = _pendingRewardRelicIds[0];
            _pendingRewardRelicIds.RemoveAt(0);
            if (string.IsNullOrWhiteSpace(serializedId))
                continue;

            try
            {
                relic = ModelDb.GetById<RelicModel>(ModelId.Deserialize(serializedId));
                if (relic != null)
                    return true;
            }
            catch
            {
                // Skip malformed entries and continue draining the queue.
            }
        }

        return false;
    }
}
