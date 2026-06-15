using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class DreamCoinExplosion : AstralPartyRelicModel
{
    private const string CoinRollContextVersion = "v1";

    [SavedProperty] public int AstralParty_DreamCoinExplosionHeadsCountThisCombat { get; set; }
    [SavedProperty] public int AstralParty_DreamCoinExplosionTailsCountThisCombat { get; set; }
    [SavedProperty] public int AstralParty_DreamCoinExplosionPendingBonusDamage { get; set; }
    [SavedProperty] public int AstralParty_DreamCoinExplosionAttackSequenceThisCombat { get; set; }
    [SavedProperty] public int AstralParty_DreamCoinExplosionPendingAttackSequence { get; set; }
    [SavedProperty] public bool AstralParty_DreamCoinExplosionPendingBonusConsumed { get; set; }

    private CardModel? _pendingAttackCard;
    private bool _isResolvingBonusDamage;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CoinFrontPower>(),
        HoverTipFactory.FromPower<CoinBackPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await ResetCombatStateAndDisplays();
    }

    public override async Task BeforeCombatStart()
    {
        await ResetCombatStateAndDisplays();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await ResetCombatStateAndDisplays();
    }

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        ClearPendingAttackState();

        if (Owner?.Creature?.CombatState == null)
            return;
        if (_isResolvingBonusDamage)
            return;
        if (cardPlay.Card.Owner != Owner || cardPlay.Card.Type != CardType.Attack)
            return;

        AstralParty_DreamCoinExplosionAttackSequenceThisCombat++;

        var goldCost = GetCurrentGoldTriggerCost();
        if (goldCost <= 0 || Owner.Gold < goldCost)
            return;

        await PersonaMultiplayerEffectHelper.LoseGoldDeterministic(goldCost, Owner, GoldLossType.Spent);

        var rolledHeads = RollHeadsForCurrentAttack(cardPlay.Card);
        if (rolledHeads)
        {
            AstralParty_DreamCoinExplosionHeadsCountThisCombat++;
            var bonusDamage = CalculatePendingBonusDamage();
            if (bonusDamage > 0)
                SetPendingAttack(cardPlay.Card, bonusDamage);
        }
        else
        {
            AstralParty_DreamCoinExplosionTailsCountThisCombat++;
        }

        await SyncCoinDisplayPowers(cardPlay.Card);
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!CanResolvePendingBonusDamage(dealer, result, target, cardSource))
            return;

        AstralParty_DreamCoinExplosionPendingBonusConsumed = true;
        var bonusDamage = AstralParty_DreamCoinExplosionPendingBonusDamage;
        if (bonusDamage <= 0)
            return;

        Flash();
        _isResolvingBonusDamage = true;
        try
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                bonusDamage,
                ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                Owner!.Creature,
                null);
        }
        finally
        {
            _isResolvingBonusDamage = false;
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card == _pendingAttackCard)
            ClearPendingAttackState();

        return Task.CompletedTask;
    }

    private int GetCurrentGoldTriggerCost()
    {
        if (Owner == null)
            return 0;

        var currentGold = Math.Max(0m, Owner.Gold);
        if (currentGold <= 0m)
            return 0;

        var rawCost = Math.Max(1m, Math.Ceiling(currentGold * 0.01m));
        return StableNumericStateHelper.ClampCeilingToInt(rawCost, 1m, int.MaxValue);
    }

    private bool RollHeadsForCurrentAttack(CardModel card)
    {
        var combatState = Owner?.Creature?.CombatState;
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            2,
            MainFile.ModId,
            Id.Entry,
            nameof(DreamCoinExplosion),
            "coin",
            CoinRollContextVersion,
            Owner?.RunState?.Rng.StringSeed ?? string.Empty,
            Owner?.NetId ?? 0UL,
            combatState?.RoundNumber ?? 0,
            AstralParty_DreamCoinExplosionAttackSequenceThisCombat,
            card.Id.Entry);
        return roll == 0;
    }

    private int CalculatePendingBonusDamage()
    {
        var netHeads = Math.Max(
            0,
            AstralParty_DreamCoinExplosionHeadsCountThisCombat - AstralParty_DreamCoinExplosionTailsCountThisCombat);
        if (netHeads <= 0)
            return 0;

        decimal bonusDamage = 1m;
        for (var index = 1; index < netHeads && bonusDamage < int.MaxValue; index++)
            bonusDamage *= 2m;

        return StableNumericStateHelper.ClampCeilingToInt(bonusDamage, 0m, int.MaxValue);
    }

    private bool CanResolvePendingBonusDamage(
        Creature? dealer,
        DamageResult result,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (_isResolvingBonusDamage)
            return false;
        if (_pendingAttackCard == null || AstralParty_DreamCoinExplosionPendingBonusDamage <= 0)
            return false;
        if (AstralParty_DreamCoinExplosionPendingBonusConsumed)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target.Side == Owner.Creature.Side)
            return false;
        if (cardSource != _pendingAttackCard)
            return false;

        return result.TotalDamage > 0m;
    }

    private async Task ResetCombatStateAndDisplays()
    {
        AstralParty_DreamCoinExplosionHeadsCountThisCombat = 0;
        AstralParty_DreamCoinExplosionTailsCountThisCombat = 0;
        AstralParty_DreamCoinExplosionPendingBonusDamage = 0;
        AstralParty_DreamCoinExplosionAttackSequenceThisCombat = 0;
        AstralParty_DreamCoinExplosionPendingAttackSequence = 0;
        AstralParty_DreamCoinExplosionPendingBonusConsumed = false;
        ClearPendingAttackState();
        _isResolvingBonusDamage = false;
        await SyncCoinDisplayPowers(null);
    }

    private void SetPendingAttack(CardModel card, int bonusDamage)
    {
        _pendingAttackCard = card;
        AstralParty_DreamCoinExplosionPendingBonusDamage = Math.Max(0, bonusDamage);
        AstralParty_DreamCoinExplosionPendingAttackSequence =
            AstralParty_DreamCoinExplosionAttackSequenceThisCombat;
        AstralParty_DreamCoinExplosionPendingBonusConsumed = false;
    }

    private void ClearPendingAttackState()
    {
        _pendingAttackCard = null;
        AstralParty_DreamCoinExplosionPendingBonusDamage = 0;
        AstralParty_DreamCoinExplosionPendingAttackSequence = 0;
        AstralParty_DreamCoinExplosionPendingBonusConsumed = false;
    }

    private async Task SyncCoinDisplayPowers(CardModel? source)
    {
        if (Owner?.Creature == null)
            return;

        await SyncCoinDisplayPower<CoinFrontPower>(AstralParty_DreamCoinExplosionHeadsCountThisCombat, source);
        await SyncCoinDisplayPower<CoinBackPower>(AstralParty_DreamCoinExplosionTailsCountThisCombat, source);
    }

    private async Task SyncCoinDisplayPower<TPower>(int amount, CardModel? source)
        where TPower : PowerModel
    {
        if (Owner?.Creature == null)
            return;

        var existingPower = Owner.Creature.GetPower<TPower>();
        if (amount <= 0)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);
            return;
        }

        await PowerCmd.SetAmount<TPower>(Owner.Creature, amount, Owner.Creature, source);
    }
}
