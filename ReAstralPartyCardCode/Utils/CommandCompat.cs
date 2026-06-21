using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PowerCmdCompat
{
    private static readonly PlayerChoiceContext DefaultChoiceContext = new ThrowingPlayerChoiceContext();

    public static Task<TPower?> Apply<TPower>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
        where TPower : PowerModel
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<TPower>(
            DefaultChoiceContext,
            target,
            amount,
            applier,
            cardSource,
            silent);
    }

    public static Task Apply(
        PowerModel power,
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.Apply(
            DefaultChoiceContext,
            power,
            target,
            amount,
            applier,
            cardSource,
            silent);
    }

    public static Task SetAmount<TPower>(
        Creature target,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
        where TPower : PowerModel
    {
        var existing = target.GetPower<TPower>();
        if (amount <= 0m)
            return existing == null
                ? Task.CompletedTask
                : MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(existing);

        if (existing == null)
            return Apply<TPower>(target, amount, applier, cardSource, false);

        var delta = amount - existing.Amount;
        return delta == 0m
            ? Task.CompletedTask
            : MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount(
                DefaultChoiceContext,
                existing,
                delta,
                applier,
                cardSource,
                true);
    }

    public static Task<int> ModifyAmount(
        PowerModel power,
        decimal offset,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.ModifyAmount(
            DefaultChoiceContext,
            power,
            offset,
            applier,
            cardSource,
            silent);
    }

    public static Task Decrement(PowerModel power)
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.Decrement(power);
    }

    public static Task Remove(PowerModel power)
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
    }

    public static Task Remove(Creature creature)
    {
        return RemoveAllPowersAsync(creature);
    }

    public static Task Remove<TPower>(Creature creature)
        where TPower : PowerModel
    {
        var power = creature.GetPower<TPower>();
        return power == null
            ? Task.CompletedTask
            : MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
    }

    public static Task TickDownDuration(PowerModel power)
    {
        return MegaCrit.Sts2.Core.Commands.PowerCmd.TickDownDuration(power);
    }

    private static async Task RemoveAllPowersAsync(Creature creature)
    {
        foreach (var power in creature.Powers.ToList())
            await MegaCrit.Sts2.Core.Commands.PowerCmd.Remove(power);
    }
}

public static class CreatureCmdCompat
{
    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(
            choiceContext,
            target,
            amount,
            props,
            dealer: null,
            cardSource);
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature dealer)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(choiceContext, target, amount, props, dealer);
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        // Always force the 6-arg overload here. The 5-arg cardSource overload dereferences cardSource.Owner
        // and is unsafe when callers intentionally pass a null source for effect/self/debuff damage.
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(
            choiceContext,
            target,
            amount,
            props,
            dealer,
            cardSource);
    }

    public static Task<IEnumerable<DamageResult>> Damage(
        PlayerChoiceContext choiceContext,
        IEnumerable<Creature> targets,
        decimal amount,
        ValueProp props,
        Creature dealer,
        CardModel? cardSource)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(choiceContext, targets, amount, props, dealer, cardSource);
    }

    public static Task GainBlock(
        Creature creature,
        decimal amount,
        ValueProp props,
        CardPlay? cardSource,
        bool silent = false)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.GainBlock(creature, amount, props, cardSource, silent);
    }

    public static Task LoseBlock(Creature creature, decimal amount)
    {
        creature.LoseBlockInternal(amount);
        return Task.CompletedTask;
    }

    public static Task GainMaxHp(Creature creature, decimal amount)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.GainMaxHp(creature, amount);
    }

    public static Task Heal(Creature creature, decimal amount, bool playAnim = true)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Heal(creature, amount, playAnim);
    }

    public static Task LoseMaxHp(
        PlayerChoiceContext choiceContext,
        Creature creature,
        decimal amount,
        bool isFromCard)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.LoseMaxHp(choiceContext, creature, amount, isFromCard);
    }

    public static Task SetCurrentHp(Creature creature, decimal amount)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.SetCurrentHp(creature, amount);
    }

    public static Task Stun(Creature creature)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Stun(creature, null!);
    }

    public static Task Stun(Creature creature, string nextMoveId)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.Stun(creature, nextMoveId);
    }

    public static Task TriggerAnim(Creature creature, string animName, float animDelay = 0f)
    {
        return MegaCrit.Sts2.Core.Commands.CreatureCmd.TriggerAnim(creature, animName, animDelay);
    }

    public static async Task<Creature> Add(MonsterModel monster, CombatState combatState, CombatSide side, string? slot)
    {
        var creature = combatState.CreateCreature(monster, side, slot);
        combatState.AddCreature(creature);
        CombatManager.Instance.AddCreature(creature);
        NCombatRoom.Instance?.AddCreature(creature);
        await creature.AfterAddedToRoom();
        await Hook.AfterCreatureAddedToCombat(combatState, creature);
        if (creature.Monster is { } addedMonster)
        {
            await addedMonster.BeforeCombatStart();
            addedMonster.InvokeExecutionFinished();
            await addedMonster.BeforeCombatStartLate();
            addedMonster.InvokeExecutionFinished();
        }

        return creature;
    }
}
