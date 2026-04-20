using System;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonSupermanMegas : AstralPartyRelicModel
{
    private const int MaxCounter = 4;
    private const int NextTurnDrawThreshold = 5;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartCard;

    [SavedProperty]
    public int AstralParty_PersonSupermanMegasCounter
    {
        get => _counter;
        set
        {
            _counter = value;
            _hasCanonicalCounter = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonSupermanMegasPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    // Preserve legacy wire/save names so older Superman Megas runs still hydrate correctly.
    public int AncientCard
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool StarterCard
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartCard && value)
                _pendingCombatStartCard = true;
        }
    }

    public int FurCoatCoordCols
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool FurCoatCoordRows
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
        HoverTipFactory.FromCard<SkillSolarBombardment>(),
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSupermanMegasCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonSupermanMegasPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonSupermanMegasPendingCombatStartCard)
        {
            await GrantSolarBombardment();
            AstralParty_PersonSupermanMegasPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < MaxCounter)
            return;

        await GrantSolarBombardment();
        AstralParty_PersonSupermanMegasCounter = 1;
        AstralParty_PersonSupermanMegasPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null || side != Owner.Creature.Side)
            return;

        if (PileType.Hand.GetPile(Owner).Cards.Count < NextTurnDrawThreshold)
        {
            Flash();
            await PowerCmd.Apply<DrawCardsNextTurnPower>(Owner.Creature, 1m, Owner.Creature, null);
        }

        AdvanceCounter();
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        if (cardPlay.Card.Owner != Owner)
            return;

        if (cardPlay.Card.CanonicalInstance is not SkillSolarBombardment)
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSupermanMegasCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSupermanMegasCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSupermanMegasPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonSupermanMegasCounter = 1;
            AstralParty_PersonSupermanMegasPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantSolarBombardment()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSolarBombardment>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}
