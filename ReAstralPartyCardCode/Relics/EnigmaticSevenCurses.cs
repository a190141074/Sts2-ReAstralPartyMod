using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSevenCurses : AstralPartyRelicModel
{
    private const decimal MaxHpBonus = 20m;
    private const decimal DamageTakenMultiplier = 2m;
    private const decimal NonEliteBossDamageMultiplier = 0.5m;
    private const decimal BlockGainMultiplier = 0.7m;
    private const decimal ShopSelfDamagePercent = 0.07m;
    private const decimal MaxHpLossPercentOnPreventedDeath = 0.10m;

    private bool _isGrowingExistingDebuffs;

    protected override string RelicId => "enigmatic_seven_curses";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner?.Creature != null)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpBonus);
            var missingHeal = Math.Max(0m, Owner.Creature.MaxHp - Owner.Creature.CurrentHp);
            if (missingHeal > 0m)
                await CreatureCmd.Heal(Owner.Creature, missingHeal, false);
        }

        await RingOfSevenCursesHelper.EnsureRelicPairAsync<EnigmaticSevenBlessings>(Owner);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return 1m;

        if (target == Owner.Creature)
            return DamageTakenMultiplier;

        if (dealer != Owner.Creature || target == null || target.Side == Owner.Creature.Side)
            return 1m;

        if (target.CombatState?.Encounter?.RoomType is RoomType.Elite or RoomType.Boss)
            return 1m;

        return NonEliteBossDamageMultiplier;
    }

    public override decimal ModifyBlockMultiplicative(
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target == Owner?.Creature)
            return BlockGainMultiplier;
        return 1m;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner?.Creature == null || room.RoomType != RoomType.Shop)
            return;

        var damage = Math.Max(1m, Math.Ceiling(Owner.Creature.CurrentHp * ShopSelfDamagePercent));
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            damage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (_isGrowingExistingDebuffs)
            return false;
        if (Owner?.Creature == null || target != Owner.Creature)
            return false;
        if (amount <= 0m)
            return false;
        if (canonicalPower.Type != PowerType.Debuff || canonicalPower.StackType != PowerStackType.Counter)
            return false;

        modifiedAmount += 1m;
        Flash();
        return true;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        var debuffs = Owner.Creature.Powers
            .Where(power => power.Type == PowerType.Debuff && power.StackType == PowerStackType.Counter)
            .OrderBy(power => power.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (debuffs.Count == 0)
            return;

        _isGrowingExistingDebuffs = true;
        try
        {
            foreach (var debuff in debuffs)
            {
                var canonicalPower = ModelDb.GetById<PowerModel>(debuff.Id);
                await PowerCmd.Apply(
                    canonicalPower.ToMutable(),
                    Owner.Creature,
                    1m,
                    debuff.Applier,
                    null,
                    false);
            }
        }
        finally
        {
            _isGrowingExistingDebuffs = false;
        }
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner?.Creature)
            return;

        var maxHpLoss = Math.Max(1m, Math.Ceiling(creature.MaxHp * MaxHpLossPercentOnPreventedDeath));
        await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(), creature, maxHpLoss, false);
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner)
            return false;

        var healOptions = options
            .OfType<HealRestSiteOption>()
            .ToList();
        foreach (var option in healOptions)
            options.Remove(option);

        return healOptions.Count > 0;
    }
}
