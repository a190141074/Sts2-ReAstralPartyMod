using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class EtheriumAxeDiscountPower : AstralPartyPowerModel
{
    private sealed class DiscountEntry
    {
        public required CardModel Card { get; init; }
        public required decimal DiscountAmount { get; init; }
    }

    private sealed class Data
    {
        public List<DiscountEntry> Entries { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 0;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public void IncreaseDiscount(CardModel card, decimal amount)
    {
        if (amount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var existing = data.Entries.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing == null)
        {
            data.Entries.Add(new DiscountEntry
            {
                Card = card,
                DiscountAmount = amount
            });
        }
        else
        {
            data.Entries.Remove(existing);
            data.Entries.Add(new DiscountEntry
            {
                Card = card,
                DiscountAmount = existing.DiscountAmount + amount
            });
        }

        card.InvokeEnergyCostChanged();
    }

    public decimal GetDiscount(CardModel? card)
    {
        if (card == null)
            return 0m;

        return GetInternalData<Data>().Entries
            .FirstOrDefault(entry => ReferenceEquals(entry.Card, card))
            ?.DiscountAmount ?? 0m;
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
        if (card.EnergyCost.CostsX)
            return false;

        var discount = GetDiscount(card);
        if (discount <= 0m)
            return false;

        modifiedCost = Math.Max(0m, originalCost - discount);
        return modifiedCost != originalCost;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null || cardPlay.Card.Owner != Owner.Player)
            return;

        RemoveTrackedCard(cardPlay.Card);
        if (!GetInternalData<Data>().Entries.Any())
            await PowerCmd.Remove(this);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Pile?.Type == PileType.Hand)
            return;

        RemoveTrackedCard(card);
        if (!GetInternalData<Data>().Entries.Any())
            await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        foreach (var entry in GetInternalData<Data>().Entries)
            entry.Card.InvokeEnergyCostChanged();

        GetInternalData<Data>().Entries.Clear();
        return Task.CompletedTask;
    }

    private void RemoveTrackedCard(CardModel card)
    {
        var data = GetInternalData<Data>();
        var existing = data.Entries.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing == null)
            return;

        data.Entries.Remove(existing);
        card.InvokeEnergyCostChanged();
    }
}
