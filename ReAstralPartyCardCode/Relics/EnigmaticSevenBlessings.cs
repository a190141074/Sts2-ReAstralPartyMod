using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.RestSite;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSevenBlessings : AstralPartyRelicModel
{
    private const int PotionSlotsBonus = 4;
    private const int SmithBonus = 4;
    private const int BaseSpecialMaterialDropPermille = 300;
    private const int SpecialMaterialDropPermillePerOtherPlayer = 10;
    private const int SpecialMaterialDropPermillePerMissStreak = 140;
    private const int RelicDropPermille = 330;
    private const int ExtraCardRewardPermille = 115;
    private readonly List<string> _pendingUniqueMaterialRewardKeys = [];

    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsUniqueMaterialMissStreak { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsEnemyDeathSpecialMaterialRollSequence { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsCombatEndSpecialMaterialRollSequence { get; set; }
    [SavedProperty] public bool AstralParty_SevenCursesMaxHpBonusGranted { get; set; }
    [SavedProperty] public bool AstralParty_SevenBlessingsPotionSlotsGranted { get; set; }
    [SavedProperty]
    private string AstralParty_EnigmaticSevenBlessingsPendingUniqueMaterialRewardsSerialized
    {
        get => string.Join("|", _pendingUniqueMaterialRewardKeys);
        set
        {
            _pendingUniqueMaterialRewardKeys.Clear();
            if (string.IsNullOrWhiteSpace(value))
                return;

            foreach (var key in value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _pendingUniqueMaterialRewardKeys.Add(key);
        }
    }

    protected override string RelicId => "enigmatic_seven_blessings";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (RingOfSevenCursesHelper.ShouldGrantSevenBlessingsPotionSlots(Owner, this))
        {
            await PlayerCmd.GainMaxPotionCount(PotionSlotsBonus, Owner);
            RingOfSevenCursesHelper.GrantSevenBlessingsRandomPotions(Owner, PotionSlotsBonus);
            RingOfSevenCursesHelper.MarkSevenBlessingsPotionSlotsGranted(Owner);
        }

        await RingOfSevenCursesHelper.EnsureRelicPairAsync<EnigmaticSevenCurses>(Owner);
        RingOfSevenCursesHelper.SyncSeriesRewardFlags(Owner);
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount = 0;
        _pendingUniqueMaterialRewardKeys.Clear();
        AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsEnemyDeathSpecialMaterialRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsCombatEndSpecialMaterialRollSequence = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature target,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || Owner?.Creature == null)
            return;
        if (target.Side == Owner.Creature.Side)
            return;

        var sequence = AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence++;
        var didDropRelic = RingOfSevenCursesHelper.RollPermille(
            RelicDropPermille,
            MainFile.ModId,
            RingOfSevenCursesHelper.SeriesId,
            RelicId,
            "enemy_death_relic",
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.RunState.TotalFloor,
            Owner.NetId,
            sequence);
        if (didDropRelic)
        {
            AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount++;
            Flash();
        }

        TryQueueEnemyDeathSpecialMaterialReward();
        await Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return;
        if (!result.WasTargetKilled || target.Side == Owner.Creature.Side)
            return;

        var sequence = AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence++;
        var didGrantCardReward = RingOfSevenCursesHelper.RollPermille(
            ExtraCardRewardPermille,
            MainFile.ModId,
            RingOfSevenCursesHelper.SeriesId,
            RelicId,
            "self_kill_card_reward",
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.RunState.TotalFloor,
            Owner.NetId,
            sequence);
        if (!didGrantCardReward)
            return;

        AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount++;
        Flash();
        await Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        TryQueueCombatEndSpecialMaterialReward();

        for (var i = 0; i < AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount; i++)
            room.AddExtraReward(Owner, new RelicReward(Owner));

        for (var i = 0; i < AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount; i++)
            room.AddExtraReward(Owner, new CardReward(CardCreationOptions.ForRoom(Owner, room.RoomType), 3, Owner));

        AddPendingSpecialMaterialRewards(room);

        AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount = 0;
        _pendingUniqueMaterialRewardKeys.Clear();
        AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsEnemyDeathSpecialMaterialRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsCombatEndSpecialMaterialRollSequence = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await RingOfSevenCursesHelper.EnsureSeriesIntegrityAsync(Owner);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;

        var modified = false;
        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption != null)
        {
            smithOption.SmithCount += SmithBonus;
            modified = true;
        }

        if (options.All(static option => option is not EnigmaticSynthesisRestSiteOption))
        {
            options.Add(new EnigmaticSynthesisRestSiteOption(player));
            modified = true;
        }

        return modified;
    }

    public override bool TryModifyCardRewardOptions(
        Player player,
        List<CardCreationResult> rewardCards,
        CardCreationOptions options)
    {
        if (player != Owner)
            return false;

        return RingOfSevenCursesHelper.TryAppendHigherRarityRewardCard(player, rewardCards, options);
    }

    private void TryQueueEnemyDeathSpecialMaterialReward()
    {
        if (Owner?.RunState == null)
            return;

        var sequence = AstralParty_EnigmaticSevenBlessingsEnemyDeathSpecialMaterialRollSequence++;
        if (!TryQueueSpecialMaterialReward(
            "enemy_death_special_material",
            "enemy_death_special_material_kind",
            "enemy_death_special_material_amount",
            sequence))
            return;

        Flash();
    }

    private void TryQueueCombatEndSpecialMaterialReward()
    {
        if (Owner?.RunState == null)
            return;

        var sequence = AstralParty_EnigmaticSevenBlessingsCombatEndSpecialMaterialRollSequence++;
        if (!TryQueueSpecialMaterialReward(
            "combat_end_special_material",
            "combat_end_special_material_kind",
            "combat_end_special_material_amount",
            sequence))
            return;

        Flash();
    }

    private bool TryQueueSpecialMaterialReward(
        string dropSalt,
        string kindSalt,
        string amountSalt,
        int sequence)
    {
        if (Owner?.RunState == null)
            return false;

        var dropPermille = GetCurrentSpecialMaterialDropPermille();
        var didDropSpecialMaterial = RingOfSevenCursesHelper.RollPermille(
            dropPermille,
            MainFile.ModId,
            RingOfSevenCursesHelper.SeriesId,
            RelicId,
            dropSalt,
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.RunState.TotalFloor,
            Owner.NetId,
            sequence);
        if (!didDropSpecialMaterial)
        {
            AstralParty_EnigmaticSevenBlessingsUniqueMaterialMissStreak++;
            return false;
        }

        var kind = EnigmaticRewardRegistry.RollUniqueMaterialKind(
            MainFile.ModId,
            RingOfSevenCursesHelper.SeriesId,
            RelicId,
            kindSalt,
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.RunState.TotalFloor,
            Owner.NetId,
            sequence);
        var amount = EnigmaticRewardRegistry.RollRewardAmount(
            kind,
            MainFile.ModId,
            RingOfSevenCursesHelper.SeriesId,
            RelicId,
            amountSalt,
            Owner.RunState.Rng.StringSeed,
            Owner.RunState.CurrentActIndex,
            Owner.RunState.TotalFloor,
            Owner.NetId,
            sequence);
        _pendingUniqueMaterialRewardKeys.Add(EnigmaticRewardRegistry.CreateRewardKey(kind, amount));
        AstralParty_EnigmaticSevenBlessingsUniqueMaterialMissStreak = 0;
        return true;
    }

    private int GetCurrentSpecialMaterialDropPermille()
    {
        var otherPlayerCount = Math.Max(0, (Owner?.RunState?.Players.Count ?? 1) - 1);
        var thresholdPermille =
            BaseSpecialMaterialDropPermille
            + otherPlayerCount * SpecialMaterialDropPermillePerOtherPlayer
            + AstralParty_EnigmaticSevenBlessingsUniqueMaterialMissStreak * SpecialMaterialDropPermillePerMissStreak;
        return Math.Clamp(thresholdPermille, 0, 1000);
    }

    private void AddPendingSpecialMaterialRewards(CombatRoom room)
    {
        if (Owner == null || _pendingUniqueMaterialRewardKeys.Count == 0)
            return;

        foreach (var rewardKey in _pendingUniqueMaterialRewardKeys)
        {
            if (!EnigmaticRewardRegistry.TryParseRewardKey(rewardKey, out var kind, out var amount))
                continue;

            room.AddExtraReward(Owner, EnigmaticRewardRegistry.CreateUniqueMaterialReward(Owner, kind, amount));
        }
    }
}
