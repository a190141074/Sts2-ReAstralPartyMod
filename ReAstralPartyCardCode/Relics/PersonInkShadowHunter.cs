using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonInkShadowHunter : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;
    private const int TwinShadowDuration = 2;
    private const int MinTrackedAttackCost = 1;
    private const int MaxTrackedAttackCost = 3;

    [SavedProperty] public int AstralParty_PersonInkShadowHunterCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonInkShadowHunterPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn { get; set; }

    [SavedProperty] public int AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillShadowFusion>(),
        HoverTipFactory.FromPower<TwinShadowsPower>(),
        HoverTipFactory.FromPower<ShadowsLimitPower>(),
        HoverTipFactory.FromPower<HuntersFeastPower>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonInkShadowHunterCounter = 1;
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;
        AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus = 0;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonInkShadowHunterPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner?.Creature == null)
            return;

        var feastAmount = room.RoomType switch
        {
            RoomType.Elite => 1m,
            RoomType.Boss => 3m,
            _ => 0m
        };
        if (feastAmount <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<HuntersFeastPower>(Owner.Creature, feastAmount, Owner.Creature, null);
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        if (AstralParty_PersonInkShadowHunterPendingCombatStartCard)
        {
            await GrantShadowFusion();
            AstralParty_PersonInkShadowHunterPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantShadowFusion();
        AstralParty_PersonInkShadowHunterCounter = 1;
        AstralParty_PersonInkShadowHunterPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        // Keep non-card turn-start effects after the normal draw.
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;
        await SyncAttackLimitPower();
        await ApplyTwinShadowToRandomEnemy();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        AdvanceCounter();
        await SetAttackLimitPowerRemaining(0);
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        if (cardPlay.Card.Owner != Owner)
            return;
        if (cardPlay.Card.Type != CardType.Attack)
            return;
        if (cardPlay.Target == null || cardPlay.Target.Side == Owner.Creature.Side || !cardPlay.Target.IsAlive)
            return;

        var energyValue = cardPlay.Resources.EnergyValue;
        if (energyValue < MinTrackedAttackCost || energyValue > MaxTrackedAttackCost)
            return;
        if (!CanTriggerAttackShadowThisTurn())
            return;

        await ApplyTwinShadow(cardPlay.Target);
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn++;
        await SyncAttackLimitPower();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature != null)
        {
            var huntersFeastAmount = (int)Owner.Creature.GetPowerAmount<HuntersFeastPower>();
            if (huntersFeastAmount > 0)
            {
                AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus += huntersFeastAmount;
                Flash();
            }
        }

        AdvanceCounterAfterCombatEnd();
        await RemoveAttackLimitPower();
        InvokeDisplayAmountChanged();
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonInkShadowHunterCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonInkShadowHunterCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;

        if (AstralParty_PersonInkShadowHunterPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonInkShadowHunterCounter = 1;
            AstralParty_PersonInkShadowHunterPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantShadowFusion()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillShadowFusion>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private async Task ApplyTwinShadowToRandomEnemy()
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        var enemies = CombatTargetOrdering.GetLivingOpponentsStable(Owner.Creature);
        if (enemies.Count == 0)
            return;

        var targetIndex = Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count);
        var target = enemies[targetIndex];

        await ApplyTwinShadow(target);
    }

    private async Task ApplyTwinShadow(Creature target)
    {
        if (Owner?.Creature == null || !target.IsAlive)
            return;

        Flash();
        await PowerCmd.Apply<TwinShadowsPower>(target, TwinShadowDuration, Owner.Creature, null);
    }

    private bool CanTriggerAttackShadowThisTurn()
    {
        return GetRemainingAttackShadowTriggersThisTurn() > 0;
    }

    private int GetRemainingAttackShadowTriggersThisTurn()
    {
        var playerCombatState = Owner?.PlayerCombatState;
        var currentMaxEnergy = playerCombatState?.MaxEnergy ?? Owner?.MaxEnergy ?? 0;
        var totalQuota = currentMaxEnergy + AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus;
        return Math.Max(totalQuota - AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn, 0);
    }

    private async Task SyncAttackLimitPower()
    {
        await SetAttackLimitPowerRemaining(GetRemainingAttackShadowTriggersThisTurn());
    }

    private async Task SetAttackLimitPowerRemaining(int remainingUses)
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        var storedAmount = Math.Max(remainingUses, 0) + 1;
        await PowerCmd.SetAmount<ShadowsLimitPower>(Owner.Creature, storedAmount, Owner.Creature, null);
    }

    private async Task RemoveAttackLimitPower()
    {
        if (Owner?.Creature == null || !Owner.Creature.HasPower<ShadowsLimitPower>())
            return;

        await PowerCmd.Remove(Owner.Creature.GetPower<ShadowsLimitPower>());
    }
}
