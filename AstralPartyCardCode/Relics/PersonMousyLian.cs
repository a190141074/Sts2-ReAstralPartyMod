using System;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonMousyLian : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartCard;

    [SavedProperty]
    public int AstralParty_PersonMousyLianCounter
    {
        get => _counter;
        set
        {
            _counter = value;
            _hasCanonicalCounter = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonMousyLianPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    // Preserve legacy wire/save names so mixed builds and older saves resolve to the same state.
    public int CurrentBlock
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool IncreasedBlock
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartCard && value)
                _pendingCombatStartCard = true;
        }
    }

    public int CombatsSeen
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool TinkerTimeType
    {
        get => default;
        set
        {
            if (!_hasCanonicalPendingCombatStartCard && value)
                _pendingCombatStartCard = true;
        }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSaveMeMousy>()
    ];

    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonMousyLianCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonMousyLianPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonMousyLianPendingCombatStartCard)
        {
            await GrantSaveMeMousy();
            AstralParty_PersonMousyLianPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantSaveMeMousy();
        AstralParty_PersonMousyLianCounter = 1;
        AstralParty_PersonMousyLianPendingCombatStartCard = false;
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

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonMousyLianCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonMousyLianCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonMousyLianPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonMousyLianCounter = 1;
            AstralParty_PersonMousyLianPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantSaveMeMousy()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSaveMeMousy>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
