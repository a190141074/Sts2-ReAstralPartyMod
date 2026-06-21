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
public class PersonInkShadowHunter : CooldownPersonRelicBase
{
    private const int InkShadowHunterBaseMaxCounter = 4;
    private const int TwinShadowDuration = 2;
    private const int MinTrackedAttackCost = 1;
    private const int MaxTrackedAttackCost = 3;

    [SavedProperty] public int AstralParty_PersonInkShadowHunterCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonInkShadowHunterPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn { get; set; }

    [SavedProperty] public int AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonInkShadowHunterCounter;
        set => AstralParty_PersonInkShadowHunterCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonInkShadowHunterPendingCombatStartCard;
        set => AstralParty_PersonInkShadowHunterPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillShadowFusion>(),
        HoverTipFactory.FromPower<TwinShadowsPower>(),
        HoverTipFactory.FromPower<ShadowsLimitPower>(),
        HoverTipFactory.FromPower<HuntersFeastPower>()
    ];

    protected override int BaseMaxCounter => InkShadowHunterBaseMaxCounter;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;
        AstralParty_PersonInkShadowHunterPermanentAttackQuotaBonus = 0;
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

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return;

        // Keep non-card turn-start effects after the normal draw.
        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;
        await SyncAttackLimitPower();
        await ApplyTwinShadowToRandomEnemy();
    }

    protected override Task AfterAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return SetAttackLimitPowerRemaining(0);
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

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
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

        AstralParty_PersonInkShadowHunterAttackTriggerCountThisTurn = 0;
        return Task.CompletedTask;
    }

    protected override Task AfterAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        return RemoveAttackLimitPower();
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillShadowFusion>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
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
