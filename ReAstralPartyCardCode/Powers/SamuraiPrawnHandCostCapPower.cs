using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class SamuraiPrawnHandCostCapPower : AstralPartyPowerModel
{
    private sealed class TrackedCardEntry
    {
        public required CardModel Card { get; init; }
        public required decimal EnergyDiscountAmount { get; init; }
        public required decimal StarDiscountAmount { get; init; }
    }

    private sealed class Data
    {
        public List<TrackedCardEntry> TrackedCards { get; } = [];
    }

    private const decimal CostCap = 3m;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 0;

    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public override LocString Title => ModelDb.Relic<PersonSamuraiPrawn>().Title;

    public override LocString Description => ModelDb.Relic<PersonSamuraiPrawn>().DynamicDescription;

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return ModelDb.Relic<PersonSamuraiPrawn>().PackedIconPath;

        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (Owner?.Player == null)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (card.Type != CardType.Attack || card.EnergyCost.CostsX)
            return false;

        var entry = FindTrackedCard(card);
        if (entry == null)
            return false;

        modifiedCost = Math.Max(0m, originalCost - entry.EnergyDiscountAmount);
        return modifiedCost != originalCost;
    }

    public override bool TryModifyStarCost(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (Owner?.Player == null)
            return false;
        if (card.Owner != Owner.Player)
            return false;
        if (card.Pile?.Type != PileType.Hand)
            return false;
        if (card.Type != CardType.Attack || card.HasStarCostX)
            return false;

        var entry = FindTrackedCard(card);
        if (entry == null || entry.StarDiscountAmount <= 0m)
            return false;

        modifiedCost = Math.Max(0m, originalCost - entry.StarDiscountAmount);
        return modifiedCost != originalCost;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner?.Player == null)
            return;
        if (card.Owner != Owner.Player)
            return;

        if (card.Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
        {
            TrackCardIfNeeded(card);
            return;
        }

        if (oldPileType != PileType.Hand)
            return;

        RemoveTrackedCard(card);
        if (!GetInternalData<Data>().TrackedCards.Any())
            await PowerCmd.Remove(this);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        RemoveTrackedCard(cardPlay.Card);
        if (!GetInternalData<Data>().TrackedCards.Any())
            await PowerCmd.Remove(this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        foreach (var entry in GetInternalData<Data>().TrackedCards)
            entry.Card.InvokeEnergyCostChanged();

        GetInternalData<Data>().TrackedCards.Clear();
        return Task.CompletedTask;
    }

    private void TrackCardIfNeeded(CardModel card)
    {
        RemoveTrackedCard(card, false);

        if (card.Type != CardType.Attack)
            return;

        decimal energyDiscountAmount = 0m;
        if (!card.EnergyCost.CostsX)
        {
            var currentEnergyCost = card.EnergyCost.GetResolved();
            if (currentEnergyCost > CostCap)
                energyDiscountAmount = currentEnergyCost - CostCap;
        }

        decimal starDiscountAmount = 0m;
        if (!card.HasStarCostX)
        {
            var currentStarCost = card.GetStarCostWithModifiers();
            if (currentStarCost > CostCap)
                starDiscountAmount = currentStarCost - CostCap;
        }

        if (energyDiscountAmount <= 0m && starDiscountAmount <= 0m)
            return;

        GetInternalData<Data>().TrackedCards.Add(new TrackedCardEntry
        {
            Card = card,
            EnergyDiscountAmount = energyDiscountAmount,
            StarDiscountAmount = starDiscountAmount
        });
        card.InvokeEnergyCostChanged();
    }

    private TrackedCardEntry? FindTrackedCard(CardModel card)
    {
        return GetInternalData<Data>().TrackedCards
            .FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
    }

    private void RemoveTrackedCard(CardModel card, bool invokeEnergyChange = true)
    {
        var data = GetInternalData<Data>();
        var existing = data.TrackedCards.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing == null)
            return;

        data.TrackedCards.Remove(existing);
        if (invokeEnergyChange)
            card.InvokeEnergyCostChanged();
    }
}
