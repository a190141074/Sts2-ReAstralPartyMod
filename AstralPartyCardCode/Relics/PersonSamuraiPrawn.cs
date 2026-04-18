using System;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
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
public class PersonSamuraiPrawn : AstralPartyRelicModel
{
    private const int MaxCounter = 4;

    [SavedProperty] public int AstralParty_PersonSamuraiPrawnCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnPendingCombatStartCard { get; set; }

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnFirstAttackTriggered { get; set; }

    [SavedProperty] public int AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFamousBlade>()
    ];

    // Use the real cooldown value instead of a modulo view of an ever-growing counter.
    public override int DisplayAmount => GetClampedCounter();

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSamuraiPrawnCounter = 1;
        // Newly obtained cooldown relics should grant their first card on the next combat start.
        AstralParty_PersonSamuraiPrawnPendingCombatStartCard = true;
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;

        if (Owner?.Creature?.CombatState == null || !AstralParty_PersonSamuraiPrawnPendingCombatStartCard)
            return;

        await GrantFamousBlade();
        AstralParty_PersonSamuraiPrawnPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;

        // Reaching the cap means the relic is ready to generate its card and restart the cooldown.
        if (GetClampedCounter() < MaxCounter)
            return;

        await GrantFamousBlade();
        AstralParty_PersonSamuraiPrawnCounter = 1;
        AstralParty_PersonSamuraiPrawnPendingCombatStartCard = false;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null)
            return;

        if (AstralParty_PersonSamuraiPrawnFirstAttackTriggered)
            return;

        if (command.Attacker != Owner.Creature || command.TargetSide == Owner.Creature.Side)
            return;

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = true;

        if (Owner.Creature.GetPowerAmount<SwordAuraPower>() >= 3)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<SwordAuraPower>().ToMutable(),
            Owner.Creature,
            1,
            Owner.Creature,
            null,
            false
        );
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
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        AdvanceCounterAfterCombatEnd();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public int GetFamousBladeDamage()
    {
        return 1 + AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura / 2;
    }

    public void IncreaseFamousBladeConsumedAura(int amount)
    {
        if (amount <= 0)
            return;

        AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura += amount;
    }

    private int GetClampedCounter()
    {
        return Math.Clamp(AstralParty_PersonSamuraiPrawnCounter, 1, MaxCounter);
    }

    private void AdvanceCounter()
    {
        AstralParty_PersonSamuraiPrawnCounter = Math.Min(GetClampedCounter() + 1, MaxCounter);
    }

    private void AdvanceCounterAfterCombatEnd()
    {
        if (AstralParty_PersonSamuraiPrawnPendingCombatStartCard)
            return;

        if (GetClampedCounter() >= MaxCounter - 1)
        {
            AstralParty_PersonSamuraiPrawnCounter = 1;
            AstralParty_PersonSamuraiPrawnPendingCombatStartCard = true;
            return;
        }

        AdvanceCounter();
    }

    private async Task GrantFamousBlade()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFamousBlade>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }
}