using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class NotYetActivatedDiscountPower : AstralPartyPowerModel
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

    protected override bool IsVisibleInternal => false;

    protected override object InitInternalData()
    {
        return new Data();
    }

    public void TrackCard(CardModel card, decimal discountAmount)
    {
        if (discountAmount <= 0m)
            return;

        var data = GetInternalData<Data>();
        var existing = data.Entries.FirstOrDefault(entry => ReferenceEquals(entry.Card, card));
        if (existing != null)
            data.Entries.Remove(existing);

        data.Entries.Add(new DiscountEntry
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
        if (card.EnergyCost.CostsX)
            return false;

        var entry = GetInternalData<Data>().Entries.FirstOrDefault(tracked => ReferenceEquals(tracked.Card, card));
        if (entry == null)
            return false;

        modifiedCost = Math.Max(0m, originalCost - entry.DiscountAmount);
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

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner?.Player?.PlayerCombatState == null)
            return Task.CompletedTask;

        foreach (var entry in GetInternalData<Data>().Entries)
            entry.Card.InvokeEnergyCostChanged();

        GetInternalData<Data>().Entries.Clear();
        return Task.CompletedTask;
    }

    public static async Task<NotYetActivatedDiscountPower?> GetOrCreate(Player owner)
    {
        if (owner.Creature == null)
            return null;

        var existing = owner.Creature.GetPower<NotYetActivatedDiscountPower>();
        if (existing != null)
            return existing;

        await PowerCmd.Apply<NotYetActivatedDiscountPower>(owner.Creature, 1m, owner.Creature, null, false);
        return owner.Creature.GetPower<NotYetActivatedDiscountPower>();
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
