using System.Collections.Generic;
using System.Threading.Tasks;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class StagnantCosmosPower : AstralPartyPowerModel
{
    private const decimal ProtocolCostPerTrigger = 1m;
    private const decimal BonusStagnationAmount = 1m;

    private sealed class PendingTrigger
    {
        public required CardModel Card { get; init; }
        public required decimal PaidEnergy { get; init; }
        public bool Consumed { get; set; }
    }

    private sealed class Data
    {
        public List<PendingTrigger> PendingTriggers { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

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

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (Owner == null)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner?.Creature != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card.Type is not CardType.Attack and not CardType.Skill)
            return Task.CompletedTask;

        var paidEnergy = cardPlay.Resources.EnergyValue;
        if (paidEnergy < 1m)
            return Task.CompletedTask;

        GetInternalData<Data>().PendingTriggers.Add(new PendingTrigger
        {
            Card = cardPlay.Card,
            PaidEnergy = paidEnergy
        });
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
        if (!TryGetStagnationAmount(dealer, result, target, cardSource, out var stagnationAmount))
            return;
        if (stagnationAmount <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<CosmosFreezesPower>(target, stagnationAmount, Owner, cardSource, false);
        await PowerCmd.ModifyAmount(this, -ProtocolCostPerTrigger, Owner, cardSource, true);

        if (Amount <= ProtocolCostPerTrigger)
            await PowerCmd.Remove(this);
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner != null && side == Owner.Side)
            GetInternalData<Data>().PendingTriggers.Clear();

        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var pendingTriggers = GetInternalData<Data>().PendingTriggers;
        for (var i = pendingTriggers.Count - 1; i >= 0; i--)
        {
            if (!ReferenceEquals(pendingTriggers[i].Card, cardPlay.Card))
                continue;

            pendingTriggers.RemoveAt(i);
            break;
        }

        return Task.CompletedTask;
    }

    private bool TryGetStagnationAmount(
        Creature? dealer,
        DamageResult result,
        Creature target,
        CardModel? cardSource,
        out decimal stagnationAmount)
    {
        stagnationAmount = 0m;

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
        if (Amount < ProtocolCostPerTrigger)
            return false;

        var pendingTrigger = FindPendingTrigger(cardSource);
        if (pendingTrigger == null || pendingTrigger.Consumed)
            return false;

        pendingTrigger.Consumed = true;
        stagnationAmount = pendingTrigger.PaidEnergy + BonusStagnationAmount;
        return stagnationAmount > 0m;
    }

    private PendingTrigger? FindPendingTrigger(CardModel cardSource)
    {
        var pendingTriggers = GetInternalData<Data>().PendingTriggers;
        for (var i = pendingTriggers.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(pendingTriggers[i].Card, cardSource))
                return pendingTriggers[i];
        }

        return null;
    }
}
