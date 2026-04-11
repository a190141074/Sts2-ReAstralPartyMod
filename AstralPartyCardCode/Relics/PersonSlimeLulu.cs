using System;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class PersonSlimeLulu : AstralPartyRelicModel
{
    private const int MaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonSlimeLuluCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSlimeLuluPendingCombatStartCard { get; set; }

    [SavedProperty] public int AstralParty_PersonSlimeLuluHealingSlimeUses { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => GetClampedCounter();

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        AstralParty_PersonSlimeLuluCounter = 1;
        AstralParty_PersonSlimeLuluPendingCombatStartCard = false;
        AstralParty_PersonSlimeLuluHealingSlimeUses = 0;
        RefreshCounterDisplay();

        await CreatureCmd.LoseMaxHp(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            10m,
            false
        );
    }

    public override async Task BeforeCombatStart()
    {
        if (Owner?.Creature?.CombatState == null || !AstralParty_PersonSlimeLuluPendingCombatStartCard)
            return;

        await GrantHealingSlime();
        AstralParty_PersonSlimeLuluPendingCombatStartCard = false;
        RefreshCounterDisplay();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        if (Owner?.Creature?.CombatState == null)
            return;

        if (GetClampedCounter() >= MaxCounter)
        {
            await GrantHealingSlime();
            AstralParty_PersonSlimeLuluCounter = 1;
            AstralParty_PersonSlimeLuluPendingCombatStartCard = false;
            RefreshCounterDisplay();
        }
    }

    public override Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null)
            return Task.CompletedTask;

        if (side != Owner.Creature.Side)
            return Task.CompletedTask;

        AdvanceCounter();
        RefreshCounterDisplay();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AdvanceCounterAfterCombatEnd();
        RefreshCounterDisplay();
        return Task.CompletedTask;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    )
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;

        if (result.TotalDamage <= 0)
            return;

        Flash();

        await PowerCmd.Apply<HalfLifeHealPower>(
            Owner.Creature,
            1m,
            Owner.Creature,
            null,
            false
        );

        AdvanceCounter();
        RefreshCounterDisplay();
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSlimeLuluCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSlimeLuluCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSlimeLuluPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonSlimeLuluCounter = 1;
            AstralParty_PersonSlimeLuluPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantHealingSlime()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillHealingSlime>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    private void RefreshCounterDisplay()
    {
        InvokeDisplayAmountChanged();
    }
}