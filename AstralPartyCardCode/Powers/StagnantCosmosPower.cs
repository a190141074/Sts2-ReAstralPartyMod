using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class StagnantCosmosPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public HashSet<CardModel> EligibleCards { get; } = [];
    }

    private const decimal DefaultCap = 100m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => (int)Amount;

    public override bool ShouldReceiveCombatHooks => true;

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<CosmosFreezesPower>()
    ];

    public override bool TryModifyPowerAmountReceived(
        PowerModel canonicalPower,
        Creature target,
        decimal amount,
        Creature? applier,
        out decimal modifiedAmount)
    {
        modifiedAmount = amount;

        if (canonicalPower is not StagnantCosmosPower)
            return false;
        if (target != Owner)
            return false;

        // This protocol is unique: reapplications neither stack nor refresh.
        modifiedAmount = 0m;
        return true;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner == null || Amount > 0m)
            return;

        await PowerCmd.ModifyAmount(this, DefaultCap, applier, cardSource, true);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner?.Creature != Owner)
            return Task.CompletedTask;
        if (cardPlay.Resources.EnergyValue < 1)
            return Task.CompletedTask;

        GetInternalData<Data>().EligibleCards.Add(cardPlay.Card);
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!CanApplyStagnation(dealer, result, target, cardSource))
            return;

        Flash();
        await PowerCmd.Apply<CosmosFreezesPower>(target, 1m, Owner, cardSource, false);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner != null && side == Owner.Side)
            GetInternalData<Data>().EligibleCards.Clear();

        return Task.CompletedTask;
    }

    private bool CanApplyStagnation(Creature? dealer, DamageResult result, Creature target, CardModel? cardSource)
    {
        if (Owner == null)
            return false;
        if (dealer != Owner)
            return false;
        if (cardSource == null)
            return false;
        if (target.Side == Owner.Side)
            return false;
        if (result.TotalDamage <= 0m)
            return false;

        return GetInternalData<Data>().EligibleCards.Contains(cardSource);
    }
}
