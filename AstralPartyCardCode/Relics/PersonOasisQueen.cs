using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonOasisQueen : AstralPartyRelicModel
{
    private const int MaxCounter = 4;
    private const int MaxTemporaryDamageBonus = 3;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _hasCanonicalPendingCombatStartCard;

    [SavedProperty]
    public int AstralParty_PersonOasisQueenCounter
    {
        get => _counter;
        set => _counter = value;
    }

    [SavedProperty]
    public bool AstralParty_PersonOasisQueenPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    // Preserve the original wire/save name so older Oasis Queen saves remain readable.
    public bool CurrentBlock
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartCard && value)
                _pendingCombatStartCard = true;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillRoyalPrerogative>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralTemporary)
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonOasisQueenCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonOasisQueenPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonOasisQueenPendingCombatStartCard)
        {
            await GrantRoyalPrerogative();
            AstralParty_PersonOasisQueenPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < MaxCounter)
            return;

        await GrantRoyalPrerogative();
        AstralParty_PersonOasisQueenCounter = 1;
        AstralParty_PersonOasisQueenPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature?.CombatState == null || Owner.Creature == null)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (dealer != Owner.Creature && cardSource?.Owner != Owner)
            return 0m;

        var hand = PileType.Hand.GetPile(Owner);
        var temporaryCards = hand.Cards.Count(card => card.Keywords.Contains(AstralKeywords.AstralTemporary));
        return Math.Min(temporaryCards, MaxTemporaryDamageBonus);
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonOasisQueenCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonOasisQueenCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonOasisQueenPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonOasisQueenCounter = 1;
            AstralParty_PersonOasisQueenPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantRoyalPrerogative()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillRoyalPrerogative>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}