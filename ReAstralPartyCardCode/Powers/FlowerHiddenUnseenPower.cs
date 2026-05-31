using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FlowerHiddenUnseenPower : AstralPartyPowerModel
{
    private const decimal MaxStacks = 2m;
    private const decimal BonusPercentPerStack = 0.12m;
    private const int DefaultDuration = 2;

    [SavedProperty] public int AstralParty_FlowerHiddenUnseenRemainingDuration { get; set; } = DefaultDuration;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Math.Clamp(Amount, 0m, MaxStacks);

    public override async Task AfterApplied(Creature? applier, MegaCrit.Sts2.Core.Models.CardModel? cardSource)
    {
        AstralParty_FlowerHiddenUnseenRemainingDuration = DefaultDuration;
        await base.AfterApplied(applier, cardSource);
    }

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;
        if (canonicalPower is not FlowerHiddenUnseenPower)
            return false;
        if (target != Owner || amount <= 0m)
            return false;

        AstralParty_FlowerHiddenUnseenRemainingDuration = DefaultDuration;
        modifiedAmount = Math.Clamp(amount, 0m, Math.Max(MaxStacks - Math.Clamp(Amount, 0m, MaxStacks), 0m));
        return true;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        AstralParty_FlowerHiddenUnseenRemainingDuration--;
        if (AstralParty_FlowerHiddenUnseenRemainingDuration > 0)
            return;

        await PowerCmd.Remove(this);
    }

    public static decimal GetVigilDamageBonus(Creature? target, decimal amount)
    {
        if (target == null || amount <= 0m)
            return 0m;

        var stacks = Math.Clamp(target.GetPowerAmount<FlowerHiddenUnseenPower>(), 0m, MaxStacks);
        return stacks <= 0m ? 0m : amount * stacks * BonusPercentPerStack;
    }
}
