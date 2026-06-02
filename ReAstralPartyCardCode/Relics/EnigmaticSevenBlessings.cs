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
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSevenBlessings : AstralPartyRelicModel
{
    private const int PotionSlotsBonus = 4;
    private const int SmithBonus = 4;
    private const int RelicDropPermille = 330;
    private const int ExtraCardRewardPermille = 115;
    // TODO(ring_of_seven_curses): 标记地图节点，有33%概率掉落特殊产物。
    // TODO(ring_of_seven_curses): 你可以在休息点制作和使用独特的遗物。

    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence { get; set; }
    [SavedProperty] public int AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence { get; set; }
    [SavedProperty] public bool AstralParty_SevenCursesMaxHpBonusGranted { get; set; }
    [SavedProperty] public bool AstralParty_SevenBlessingsPotionSlotsGranted { get; set; }

    protected override string RelicId => "enigmatic_seven_blessings";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (RingOfSevenCursesHelper.ShouldGrantSevenBlessingsPotionSlots(Owner, this))
        {
            await PlayerCmd.GainMaxPotionCount(PotionSlotsBonus, Owner);
            RingOfSevenCursesHelper.MarkSevenBlessingsPotionSlotsGranted(Owner);
        }

        await RingOfSevenCursesHelper.EnsureRelicPairAsync<EnigmaticSevenCurses>(Owner);
        RingOfSevenCursesHelper.SyncSeriesRewardFlags(Owner);
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence = 0;
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
            Owner.NetId,
            sequence);
        if (!didDropRelic)
            return;

        AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount++;
        Flash();
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

        for (var i = 0; i < AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount; i++)
            room.AddExtraReward(Owner, new RelicReward(Owner));

        for (var i = 0; i < AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount; i++)
            room.AddExtraReward(Owner, new CardReward(CardCreationOptions.ForRoom(Owner, room.RoomType), 3, Owner));

        AstralParty_EnigmaticSevenBlessingsPendingRelicRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsPendingExtraCardRewardCount = 0;
        AstralParty_EnigmaticSevenBlessingsEnemyDeathRollSequence = 0;
        AstralParty_EnigmaticSevenBlessingsSelfKillRollSequence = 0;
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

        var smithOption = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smithOption == null)
            return false;

        smithOption.SmithCount += SmithBonus;
        return true;
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
}
