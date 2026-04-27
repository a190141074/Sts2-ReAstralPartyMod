using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonXiaoLei : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;

    private int _counter = 1;
    private bool _pendingCombatStartCard;
    private bool _hasCanonicalCounter;
    private bool _hasCanonicalPendingCombatStartCard;

    [SavedProperty]
    public int AstralParty_PersonXiaoLeiCounter
    {
        get => _counter;
        set
        {
            _counter = value;
            _hasCanonicalCounter = true;
        }
    }

    [SavedProperty]
    public bool AstralParty_PersonXiaoLeiPendingCombatStartCard
    {
        get => _pendingCombatStartCard;
        set
        {
            _pendingCombatStartCard = value;
            _hasCanonicalPendingCombatStartCard = true;
        }
    }

    // Preserve legacy wire/save names so older XiaoLei runs still hydrate correctly.
    public int TimesLifted
    {
        get => default;
        set
        {
            if (!_hasCanonicalCounter && value != default)
                _counter = value;
        }
    }

    public bool GoldenPathAct
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

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillChainReaction>(),
        HoverTipFactory.FromPower<DragonAwakeningPower>(),
        HoverTipFactory.FromPower<TrueDragonFormPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        AstralParty_PersonXiaoLeiCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonXiaoLeiPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();

        if (Owner.GetRelic<PersonalityDerivativeXiaoLeiDragonGate>() == null)
            await RelicCmd.Obtain(
                ModelDb.Relic<PersonalityDerivativeXiaoLeiDragonGate>().ToMutable(),
                Owner
            );
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonXiaoLeiPendingCombatStartCard)
        {
            await GrantChainReaction();
            AstralParty_PersonXiaoLeiPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantChainReaction();
        AstralParty_PersonXiaoLeiCounter = 1;
        AstralParty_PersonXiaoLeiPendingCombatStartCard = false;
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

    public async Task GrantDragonAwakening(int amount)
    {
        if (amount <= 0)
            return;
        if (Owner?.Creature == null)
            return;

        Flash();
        await PowerCmd.Apply<DragonAwakeningPower>(Owner.Creature, amount, Owner.Creature, null, false);
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonXiaoLeiCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonXiaoLeiCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonXiaoLeiPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonXiaoLeiCounter = 1;
            AstralParty_PersonXiaoLeiPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantChainReaction()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillChainReaction>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
