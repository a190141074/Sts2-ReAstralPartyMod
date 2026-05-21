using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class CooldownPersonaRelicBase : PersonaRelicBase
{
    protected abstract int CounterValue { get; set; }
    protected abstract bool PendingCombatStartCard { get; set; }
    protected virtual int BaseMaxCounter => 4;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        CounterValue = 1;
        PendingCombatStartCard = true;
        OnCooldownStateInitialized();
        RefreshCooldownDisplay();
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        CombatState combatState)
    {
        var owner = Owner;
        if (owner?.Creature?.CombatState == null || side != owner.Creature.Side)
            return;

        await BeforeCooldownCardCheck(choiceContext, owner);

        if (PendingCombatStartCard)
        {
            await GrantCooldownCard();
            PendingCombatStartCard = false;
            RefreshCooldownDisplay();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantCooldownCard();
        CounterValue = 1;
        PendingCombatStartCard = false;
        RefreshCooldownDisplay();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        await BeforeAdvanceCounterOnTurnEnd(choiceContext, side);
        AdvanceCounter();
        await AfterAdvanceCounterOnTurnEnd(choiceContext, side);
        RefreshCooldownDisplay();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await BeforeAdvanceCounterAfterCombatEnd(room);
        AdvanceCounterAfterCombatEnd();
        await AfterAdvanceCounterAfterCombatEnd(room);
        RefreshCooldownDisplay();
    }

    protected virtual void OnCooldownStateInitialized()
    {
    }

    protected virtual Task BeforeCooldownCardCheck(PlayerChoiceContext choiceContext, Player player)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return Task.CompletedTask;
    }

    protected virtual Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        return Task.CompletedTask;
    }

    protected virtual Task AfterAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        return Task.CompletedTask;
    }

    protected int GetClampedCounter()
    {
        return Math.Clamp(CounterValue, 1, GetMaxCounter());
    }

    protected int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter, this);
    }

    protected void AdvanceCounter()
    {
        CounterValue = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    protected void AdvanceCounterAfterCombatEnd()
    {
        if (PendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            CounterValue = 1;
            PendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    protected void RefreshCooldownDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    protected void ReduceCooldownProgress(int amount)
    {
        if (amount <= 0 || PendingCombatStartCard)
            return;

        CounterValue = Math.Min(GetClampedCounter() + amount, GetMaxCounter());
        RefreshCooldownDisplay();
    }

    internal void AdvanceCooldownProgressFromExternalEffect(int amount)
    {
        if (amount <= 0)
            return;

        CounterValue = Math.Min(GetClampedCounter() + amount, GetMaxCounter());
        RefreshCooldownDisplay();
    }

    protected abstract Task GrantCooldownCard();
}
