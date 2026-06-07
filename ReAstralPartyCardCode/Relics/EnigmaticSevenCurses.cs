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
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

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
    private const decimal AcknowledgmentBurnAmount = 1m;
    private const decimal TwistBossDamageMultiplier = 4m;
    private const decimal TwistDebuffBonusAmount = 3m;
    private const decimal InfinitumBossDamageMultiplier = 3m;
    private const decimal InfinitumDebuffBonusAmount = 2m;
    private const decimal InfinitumLifestealPercent = 0.10m;
    private const decimal InfinitumMinDamageFloor = 8m;
    private const int InfinitumDeathProtectionPermille = 850;

    private bool _isGrowingExistingDebuffs;
    private bool _isApplyingRevelationBurn;
    private bool _isApplyingShopSelfDamage;
    private bool _pendingInfinitumDeathProtection;
    private bool _skipPreventedDeathPenaltyOnce;

    [SavedProperty] public bool AstralParty_SevenCursesMaxHpBonusGranted { get; set; }
    [SavedProperty] public bool AstralParty_SevenBlessingsPotionSlotsGranted { get; set; }
    [SavedProperty] public int AstralParty_SevenCursesInfinitumDebuffRollCounter { get; set; }
    [SavedProperty] public int AstralParty_SevenCursesInfinitumDeathRollCounter { get; set; }

    protected override string RelicId => "enigmatic_seven_curses";

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (RingOfSevenCursesHelper.ShouldGrantSevenCursesMaxHpBonus(Owner, this) && Owner?.Creature != null)
        {
            await CreatureCmd.GainMaxHp(Owner.Creature, MaxHpBonus);
            var missingHeal = Math.Max(0m, Owner.Creature.MaxHp - Owner.Creature.CurrentHp);
            if (missingHeal > 0m)
                await CreatureCmd.Heal(Owner.Creature, missingHeal, false);

            RingOfSevenCursesHelper.MarkSevenCursesMaxHpBonusGranted(Owner);
        }

        await RingOfSevenCursesHelper.EnsureRelicPairAsync<EnigmaticSevenBlessings>(Owner);
        RingOfSevenCursesHelper.SyncSeriesRewardFlags(Owner);
        CursedScrollGrabBagHelper.NormalizeForOwner(Owner);
        if (Owner != null)
        {
            await EnigmaticAcknowledgmentDeckHelper.EnsureInRunDeck(Owner);
            EnigmaticSynthesisCursedScroll.RefreshCounterForOwner(Owner);
        }
    }

    public override async Task AfterRemoved()
    {
        CursedScrollGrabBagHelper.NormalizeForOwner(Owner);
        EnigmaticSynthesisCursedScroll.RefreshCounterForOwner(Owner);
        await base.AfterRemoved();
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
        {
            if (_isApplyingShopSelfDamage)
                return 1m;
            return DamageTakenMultiplier;
        }

        if (dealer != Owner.Creature || target == null || target.Side == Owner.Creature.Side)
            return 1m;

        var revelationKind = GetRevelationInHand();
        if (revelationKind == EnigmaticRevelationKind.Twist &&
            target.CombatState?.Encounter?.RoomType == RoomType.Boss)
            return TwistBossDamageMultiplier;
        if (revelationKind == EnigmaticRevelationKind.Infinitum &&
            target.CombatState?.Encounter?.RoomType == RoomType.Boss)
            return InfinitumBossDamageMultiplier;

        if (target.CombatState?.Encounter?.RoomType is RoomType.Elite or RoomType.Boss)
            return 1m;

        if (revelationKind != EnigmaticRevelationKind.None)
            return 1m;

        return NonEliteBossDamageMultiplier;
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
        var revelationKind = GetRevelationInHand();
        if (revelationKind == EnigmaticRevelationKind.None)
            return;
        if (target.Side == Owner.Creature.Side || !target.IsAlive)
            return;
        if (result.TotalDamage <= 0m)
            return;

        if (revelationKind == EnigmaticRevelationKind.Infinitum)
        {
            Flash();

            var healAmount = Math.Max(1m, Math.Ceiling(result.TotalDamage * InfinitumLifestealPercent));
            await CreatureCmd.Heal(Owner.Creature, healAmount, false);

            if (!target.IsAlive)
                return;

            await ApplyRandomInfinitumDebuff(target, cardSource);
            return;
        }

        Flash();
        _isApplyingRevelationBurn = true;
        try
        {
            await PowerCmd.Apply<BlazingSolarBurnPower>(target, AcknowledgmentBurnAmount, Owner.Creature, cardSource, false);
        }
        finally
        {
            _isApplyingRevelationBurn = false;
        }
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

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (GetRevelationInHand() != EnigmaticRevelationKind.Infinitum)
            return 0m;

        return Math.Max(0m, InfinitumMinDamageFloor - amount);
    }

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return amount;
        if (amount <= 0m)
            return amount;

        var isDebuffDamage = SevenCursesDebuffProtectionHelper.IsDebuffDamage(target, props, dealer, cardSource);
        var isNonCombatHpLoss = target.CombatState == null;
        if (!isDebuffDamage && !isNonCombatHpLoss)
            return amount;

        var maxHpLoss = Math.Max(0m, target.CurrentHp - 1m);
        var clippedAmount = Math.Min(amount, maxHpLoss);
        if (clippedAmount >= amount)
            return amount;

        MainFile.Logger.Info(
            $"[EnigmaticSevenCurses] Prevented lethal hp loss | owner={Owner.NetId} | currentHp={target.CurrentHp} | incoming={amount} | clipped={clippedAmount} | nonCombat={isNonCombatHpLoss} | debuff={isDebuffDamage} | dealer={dealer?.ModelId.ToString() ?? "<none>"} | card={cardSource?.Id.Entry ?? "<none>"}");
        Flash();
        return clippedAmount;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await RingOfSevenCursesHelper.EnsureSeriesIntegrityAsync(Owner);

        if (Owner?.Creature == null || room.RoomType != RoomType.Shop)
            return;
        if (Owner.GetRelic<EnigmaticGemRing>() != null || EnigmaticSynthesisAvariceScroll.PreventsShopEntryDamage(Owner))
            return;

        var maxNonLethalDamage = Math.Max(0m, Owner.Creature.CurrentHp - 1m);
        if (maxNonLethalDamage <= 0m)
            return;

        var damage = Math.Max(1m, Math.Ceiling(Owner.Creature.CurrentHp * ShopSelfDamagePercent));
        damage = Math.Min(damage, maxNonLethalDamage);
        if (damage <= 0m)
            return;

        _isApplyingShopSelfDamage = true;
        try
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                Owner.Creature,
                damage,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }
        finally
        {
            _isApplyingShopSelfDamage = false;
        }
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (Owner?.Creature == null || amount <= 0m)
            return false;

        var didModify = false;
        if (!_isGrowingExistingDebuffs &&
            target == Owner.Creature &&
            StackableDebuffGrowthHelper.CanIncreaseIncomingStackableDebuff(canonicalPower, amount))
        {
            modifiedAmount += 1m;
            didModify = true;
        }

        if (!_isApplyingRevelationBurn &&
            GetRevelationInHand() == EnigmaticRevelationKind.Twist &&
            applier == Owner.Creature &&
            target.Side != Owner.Creature.Side &&
            StackableDebuffGrowthHelper.CanIncreaseIncomingStackableDebuff(canonicalPower, amount))
        {
            modifiedAmount += TwistDebuffBonusAmount;
            didModify = true;
        }

        if (GetRevelationInHand() == EnigmaticRevelationKind.Infinitum &&
            applier == Owner.Creature &&
            target.Side != Owner.Creature.Side &&
            StackableDebuffGrowthHelper.CanIncreaseIncomingStackableDebuff(canonicalPower, amount))
        {
            modifiedAmount += InfinitumDebuffBonusAmount;
            didModify = true;
        }

        if (didModify)
            Flash();
        return didModify;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature == null)
            return;

        var debuffs = Owner.Creature.Powers
            .Where(StackableDebuffGrowthHelper.CanGrowExistingStackableDebuff)
            .OrderBy(power => power.Id.Entry, StringComparer.Ordinal)
            .ToList();
        if (debuffs.Count == 0)
            return;

        _isGrowingExistingDebuffs = true;
        try
        {
            foreach (var debuff in debuffs)
                await StackableDebuffGrowthHelper.TryGrowExistingStackableDebuffAsync(debuff, 1m, null);
        }
        finally
        {
            _isGrowingExistingDebuffs = false;
        }
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner?.Creature)
            return true;
        if (GetRevelationInHand() != EnigmaticRevelationKind.Infinitum)
            return true;
        if (_pendingInfinitumDeathProtection)
            return false;

        var didPreventDeath = RingOfSevenCursesHelper.RollPermille(
            InfinitumDeathProtectionPermille,
            MainFile.ModId,
            RelicId,
            nameof(ShouldDieLate),
            "infinitum_death_protection",
            Owner.RunState?.Rng.StringSeed,
            Owner.RunState?.CurrentActIndex,
            Owner.RunState?.TotalFloor,
            Owner.NetId,
            AstralParty_SevenCursesInfinitumDeathRollCounter++);
        if (!didPreventDeath)
            return true;

        _pendingInfinitumDeathProtection = true;
        _skipPreventedDeathPenaltyOnce = true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner?.Creature)
            return;
        if (_skipPreventedDeathPenaltyOnce)
        {
            _pendingInfinitumDeathProtection = false;
            _skipPreventedDeathPenaltyOnce = false;
            Flash();
            if (creature.CurrentHp <= 0m)
                await CreatureCmd.SetCurrentHp(creature, 1m);
            return;
        }
        if (SevenCursesDebuffProtectionHelper.IsInDebuffDamageContext)
            return;
        if (EscapeScrollDeathProtectionHelper.IsActive)
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

    private EnigmaticRevelationKind GetRevelationInHand()
    {
        return EnigmaticAcknowledgmentDeckHelper.GetRevelationInHand(Owner);
    }

    private async Task ApplyRandomInfinitumDebuff(Creature target, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;

        var selectedDebuff = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            5,
            MainFile.ModId,
            RelicId,
            nameof(ApplyRandomInfinitumDebuff),
            Owner.RunState?.Rng.StringSeed,
            Owner.RunState?.CurrentActIndex,
            Owner.RunState?.TotalFloor,
            Owner.NetId,
            target.ModelId.ToString(),
            AstralParty_SevenCursesInfinitumDebuffRollCounter++);

        switch (selectedDebuff)
        {
            case 0:
                await StackableDebuffGrowthHelper.TryApplyOrGrowStackableDebuffAsync<DoomPower>(
                    target,
                    1m,
                    Owner.Creature,
                    cardSource,
                    false);
                break;
            case 1:
                await StackableDebuffGrowthHelper.TryApplyOrGrowStackableDebuffAsync<PoisonPower>(
                    target,
                    1m,
                    Owner.Creature,
                    cardSource,
                    false);
                break;
            case 2:
                await PowerCmd.Apply<VulnerablePower>(target, 1m, Owner.Creature, cardSource, false);
                break;
            case 3:
                await PowerCmd.Apply<WeakPower>(target, 1m, Owner.Creature, cardSource, false);
                break;
            default:
                await PowerCmd.Apply<BlazingSolarBurnPower>(target, 1m, Owner.Creature, cardSource, false);
                break;
        }
    }
}
