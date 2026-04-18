using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
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
public class PersonWeirdEgg : AstralPartyRelicModel
{
    private const int MaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonWeirdEggCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonWeirdEggPendingCombatStartCard { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillTroubleMaker>()
    ];

    // Keep the stored value aligned with the shown value so the cooldown is easy to reason about.
    public override int DisplayAmount => GetClampedCounter();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonWeirdEggCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next player turn start.
        AstralParty_PersonWeirdEggPendingCombatStartCard = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        if (AstralParty_PersonWeirdEggPendingCombatStartCard)
        {
            await GrantTroubleMaker();
            AstralParty_PersonWeirdEggPendingCombatStartCard = false;
            InvokeDisplayAmountChanged();
            return;
        }

        // Reaching the cap means the relic is ready to generate its card and start a fresh cycle.
        if (GetClampedCounter() < MaxCounter)
            return;

        await GrantTroubleMaker();
        AstralParty_PersonWeirdEggCounter = 1;
        AstralParty_PersonWeirdEggPendingCombatStartCard = false;
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
        return Math.Clamp(AstralParty_PersonWeirdEggCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonWeirdEggCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonWeirdEggPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonWeirdEggCounter = 1;
            AstralParty_PersonWeirdEggPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantTroubleMaker()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillTroubleMaker>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}
