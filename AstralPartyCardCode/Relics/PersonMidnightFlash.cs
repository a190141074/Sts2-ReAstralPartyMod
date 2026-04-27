using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Utils;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonMidnightFlash : AstralPartyRelicModel
{
    private const int BaseMaxCounter = 4;
    private const int CooldownReductionOnKill = 2;

    [SavedProperty] public int AstralParty_PersonMidnightFlashCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonMidnightFlashPendingCombatStartCard { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => GetClampedCounter();

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillUnstoppable>(),
        HoverTipFactory.FromPower<ReadyToStrikePower>(),
        HoverTipFactory.FromCard<Whirlwind>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonMidnightFlashCounter = 1;
        AstralParty_PersonMidnightFlashPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonMidnightFlashPendingCombatStartCard)
        {
            await GrantUnstoppable();
            AstralParty_PersonMidnightFlashPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        if (GetClampedCounter() < GetMaxCounter())
            return;

        await GrantUnstoppable();
        AstralParty_PersonMidnightFlashCounter = 1;
        AstralParty_PersonMidnightFlashPendingCombatStartCard = false;
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

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource
    )
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;
        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side)
            return Task.CompletedTask;
        if (!result.WasTargetKilled)
            return Task.CompletedTask;
        if (!Owner.Creature.HasPower<ReadyToStrikePower>())
            return Task.CompletedTask;

        Flash();
        ReduceCooldown(CooldownReductionOnKill);
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonMidnightFlashCounter, 1, GetMaxCounter());
    }

    private int GetMaxCounter()
    {
        return ExtraBatteryRelicHelper.GetAdjustedCooldownMaxCounter(Owner, BaseMaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonMidnightFlashCounter = Math.Min(GetClampedCounter() + 1, GetMaxCounter());
    }

    private void ReduceCooldown(int amount)
    {
        if (amount <= 0 || AstralParty_PersonMidnightFlashPendingCombatStartCard)
            return;

        AstralParty_PersonMidnightFlashCounter = Math.Min(GetClampedCounter() + amount, GetMaxCounter());
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonMidnightFlashPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= GetMaxCounter() - 1)
        {
            AstralParty_PersonMidnightFlashCounter = 1;
            AstralParty_PersonMidnightFlashPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantUnstoppable()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillUnstoppable>(), Owner);
        await GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, true);
    }
}
