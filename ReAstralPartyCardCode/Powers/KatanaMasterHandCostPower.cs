using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class KatanaMasterHandCostPower : AstralPartyPowerModel
{
    private sealed class Data
    {
        public List<CardModel> TrackedCards { get; } = [];
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    public override int DisplayAmount => 0;
    protected override bool IsVisibleInternal => false;
    protected override object InitInternalData() => new Data();
    public override LocString Title => ModelDb.Relic<PersonKatanaMaster>().Title;
    public override LocString Description => ModelDb.Relic<PersonKatanaMaster>().DynamicDescription;

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return ModelDb.Relic<PersonKatanaMaster>().PackedIconPath;
        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;
        if (Owner?.Player == null || card.Owner != Owner.Player || card.Pile?.Type != PileType.Hand)
            return false;
        if (card.Type != CardType.Attack || card.EnergyCost.CostsX || !GetInternalData<Data>().TrackedCards.Contains(card))
            return false;

        modifiedCost = Math.Max(0m, originalCost - 1m);
        return modifiedCost != originalCost;
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        if (Owner?.Player == null || card.Owner != Owner.Player)
            return;

        var data = GetInternalData<Data>();
        if (card.Pile?.Type == PileType.Hand && oldPileType != PileType.Hand)
        {
            if (card.Type == CardType.Attack && !card.EnergyCost.CostsX && card.EnergyCost.GetResolved() > 3m)
            {
                data.TrackedCards.Remove(card);
                data.TrackedCards.Add(card);
                card.InvokeEnergyCostChanged();
            }
            return;
        }

        if (oldPileType == PileType.Hand)
        {
            data.TrackedCards.Remove(card);
            card.InvokeEnergyCostChanged();
            if (data.TrackedCards.Count == 0)
                await PowerCmd.Remove(this);
        }
    }

    public override async Task AfterCardPlayed(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        if (Owner?.Player == null || cardPlay.Card.Owner != Owner.Player)
            return;

        var data = GetInternalData<Data>();
        data.TrackedCards.Remove(cardPlay.Card);
        cardPlay.Card.InvokeEnergyCostChanged();
        if (data.TrackedCards.Count == 0)
            await PowerCmd.Remove(this);
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await PowerCmd.Remove(this);
    }

    public override Task AfterRemoved(Creature? oldOwner)
    {
        foreach (var card in GetInternalData<Data>().TrackedCards)
            card.InvokeEnergyCostChanged();
        GetInternalData<Data>().TrackedCards.Clear();
        return Task.CompletedTask;
    }
}
