using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CyberKittyDiscountedAttackPower : AstralPartyPowerModel
{
    private sealed class DiscountedCardEntry
    {
        public required CardModel Card { get; init; }
        public required decimal DiscountAmount { get; init; }
    }

    private sealed class Data
    {
        public List<DiscountedCardEntry> DiscountedCards { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => 0;

    protected override object InitInternalData()
    {
        return new Data();
    }

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return ModelDb.Relic<PersonCyberKitty>().PackedIconPath;

        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public void TrackCard(CardModel card, decimal discountAmount)
    {
        if (discountAmount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var existing = data.DiscountedCards.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing != null) data.DiscountedCards.Remove(existing);

        data.DiscountedCards.Add(new DiscountedCardEntry
        {
            Card = card,
            DiscountAmount = discountAmount
        });
        card.InvokeEnergyCostChanged();
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

        modifiedCost = Math.Max(0m, originalCost - entry.DiscountAmount);
        return modifiedCost != originalCost;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Player == null)
            return;
        if (cardPlay.Card.Owner != Owner.Player)
            return;

        RemoveTrackedCard(cardPlay.Card);
        if (!GetInternalData<Data>().DiscountedCards.Any())
            await PowerCmd.Remove(this);
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (card.Pile?.Type == PileType.Hand)
            return;

        RemoveTrackedCard(card);
        if (!GetInternalData<Data>().DiscountedCards.Any())
            await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        foreach (var entry in GetInternalData<Data>().DiscountedCards)
            entry.Card.InvokeEnergyCostChanged();

        GetInternalData<Data>().DiscountedCards.Clear();
        return Task.CompletedTask;
    }

    private DiscountedCardEntry? FindTrackedCard(CardModel card)
    {
        return GetInternalData<Data>().DiscountedCards
            .FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
    }

    private void RemoveTrackedCard(CardModel card)
    {
        var data = GetInternalData<Data>();
        var existing = data.DiscountedCards.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing == null)
            return;

        data.DiscountedCards.Remove(existing);
        card.InvokeEnergyCostChanged();
    }
}
