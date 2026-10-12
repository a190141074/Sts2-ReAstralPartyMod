using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class TreasureBucketPower : AstralPartyPowerModel
{
    private const decimal RescueHealAmount = 5m;
    private const decimal IntangibleAmount = 99m;

    [SavedProperty] public bool AstralParty_TreasureBucketConsumed { get; set; }
    [SavedProperty] public bool AstralParty_TreasureBucketBlockLocked { get; set; }
    [SavedProperty] public ulong AstralParty_TreasureBucketLastHostileDealerNetId { get; set; }

    private bool _pendingRescue;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public bool BlocksDisabled => AstralParty_TreasureBucketBlockLocked;

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner || amount <= 0m || AstralParty_TreasureBucketConsumed)
            return amount;

        if (amount < Owner.CurrentHp)
            return amount;

        _pendingRescue = true;
        AstralParty_TreasureBucketConsumed = true;
        AstralParty_TreasureBucketBlockLocked = true;
        if (dealer?.Player != null)
            AstralParty_TreasureBucketLastHostileDealerNetId = dealer.Player.NetId;

        return Math.Max(0m, Owner.CurrentHp - 1m);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return;

        if (dealer?.Player != null)
            AstralParty_TreasureBucketLastHostileDealerNetId = dealer.Player.NetId;

        if (!_pendingRescue)
            return;

        _pendingRescue = false;
        await CreatureCmd.Heal(Owner, RescueHealAmount, true);
        await PowerCmd.Apply<IntangiblePower>(Owner, IntangibleAmount, Owner, null, false);
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (Owner == null || creature != Owner || wasRemovalPrevented)
            return Task.CompletedTask;

        var killer = ResolveKillerPlayer();
        if (killer == null || killer.RunState?.CurrentRoom is not CombatRoom room)
            return Task.CompletedTask;

        var reward = ResolveDeterministicTokenReward(killer);
        if (reward == null)
            return Task.CompletedTask;

        room.AddExtraReward(killer, new RelicReward(reward.ToMutable(), killer));
        return Task.CompletedTask;
    }

    public static bool ShouldPreventBlockGain(Creature? creature)
    {
        return creature?.GetPower<TreasureBucketPower>()?.BlocksDisabled == true;
    }

    private Player? ResolveKillerPlayer()
    {
        return Owner?.CombatState?.Players.FirstOrDefault(player =>
            player.NetId == AstralParty_TreasureBucketLastHostileDealerNetId);
    }

    private static RelicModel? ResolveDeterministicTokenReward(Player owner)
    {
        var rarityRoll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            100,
            MainFile.ModId,
            "treasure_bucket_reward",
            owner.RunState?.Rng.StringSeed,
            owner.NetId,
            owner.RunState?.TotalFloor ?? 0);
        var rarity = rarityRoll switch
        {
            < 50 => RelicRarity.Common,
            < 80 => RelicRarity.Uncommon,
            _ => RelicRarity.Rare
        };

        var candidates = TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, rarity);
        if (candidates.Count == 0)
            candidates = TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, RelicRarity.Uncommon)
                .Concat(TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, RelicRarity.Common))
                .Concat(TokenRelicRegistry.GetAvailableTokenRelicsByRarity(owner.RunState, RelicRarity.Rare))
                .DistinctBy(static relic => relic.CanonicalInstance?.Id ?? relic.Id)
                .ToList();

        return PersonMultiplayerEffectHelper.CreateDeterministicRelicChoiceOptions(
            candidates,
            1,
            MainFile.ModId,
            "treasure_bucket_reward",
            owner.NetId,
            owner.RunState?.Rng.StringSeed,
            owner.RunState?.TotalFloor ?? 0).FirstOrDefault();
    }
}
