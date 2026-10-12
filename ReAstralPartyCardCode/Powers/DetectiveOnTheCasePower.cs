using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class DetectiveOnTheCasePower : AstralPartyPowerModel
{
    public const decimal MaxStacks = 4m;
    private decimal _appliedStrength;
    private bool _playedAttackThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<StrengthPower>()];

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await SyncStrength(applier, cardSource);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power == this)
            await SyncStrength(applier, cardSource);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == cardPlay.Card.Owner && WarforgeEnchantmentHelper.CountsAsAttack(cardPlay.Card))
            _playedAttackThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        if (!_playedAttackThisTurn && Amount > 0m)
            await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
    }

    public override Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner != null && side == Owner.Side)
            _playedAttackThisTurn = false;
        return Task.CompletedTask;
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner != null && _appliedStrength != 0m)
            await PowerCmd.Apply<StrengthPower>(oldOwner, -_appliedStrength, oldOwner, null, true);
        _appliedStrength = 0m;
    }

    private async Task SyncStrength(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null)
            return;

        var desired = Math.Min(Amount, MaxStacks);
        var delta = desired - _appliedStrength;
        if (delta == 0m)
            return;

        _appliedStrength = desired;
        await PowerCmd.Apply<StrengthPower>(Owner, delta, applier, cardSource, true);
    }
}
