using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonPoisonedApple : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;
    private const decimal MarkedAttackBonusDamage = 3m;

    [SavedProperty] public int AstralParty_PersonPoisonedAppleCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonPoisonedApplePendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonPoisonedAppleInvestigationTriggerCount { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillConcealingOperation>(),
        HoverTipFactory.FromPower<InvestigationTargetPower>(),
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralInvestigationProgress)
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonPoisonedAppleCounter = 1;
        AstralParty_PersonPoisonedApplePendingCombatStartCard = true;
        AstralParty_PersonPoisonedAppleInvestigationTriggerCount = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonPoisonedApplePendingCombatStartCard)
        {
            await GrantConcealingOperation();
            AstralParty_PersonPoisonedApplePendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantConcealingOperation();
        AstralParty_PersonPoisonedAppleCounter = 1;
        AstralParty_PersonPoisonedApplePendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (!IsMarkedAttack(target, amount, dealer, cardSource))
            return 0m;

        return MarkedAttackBonusDamage;
    }

    public ConcealingInvestigationHelper.InvestigationStage GetCurrentInvestigationStage()
    {
        return ConcealingInvestigationHelper.GetInvestigationStageForTriggerCount(
            AstralParty_PersonPoisonedAppleInvestigationTriggerCount
        );
    }

    public bool HasCompletedTruthRevealProgress()
    {
        return GetCurrentInvestigationStage() == ConcealingInvestigationHelper.InvestigationStage.TruthUnveiled
               && AstralParty_PersonPoisonedAppleInvestigationTriggerCount >=
               ConcealingInvestigationHelper.GetStageAdvanceThreshold() * 4;
    }

    public void RecordInvestigationTrigger()
    {
        AstralParty_PersonPoisonedAppleInvestigationTriggerCount++;
        Flash();
    }

    private bool IsMarkedAttack(Creature? target, decimal amount, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource?.Owner != Owner)
            return false;
        if (cardSource.Type != CardType.Attack)
            return false;

        return target.GetPowerAmount<MarkLockPower>() > 0m;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonPoisonedAppleCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonPoisonedAppleCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonPoisonedApplePendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonPoisonedAppleCounter = 1;
            AstralParty_PersonPoisonedApplePendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantConcealingOperation()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillConcealingOperation>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
