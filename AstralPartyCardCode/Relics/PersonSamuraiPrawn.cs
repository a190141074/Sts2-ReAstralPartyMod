using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.cards;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class PersonSamuraiPrawn : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonSamuraiPrawnCounter { get; set; } = 1;

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnOpenedThisCombat { get; set; }

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnFirstAttackTriggered { get; set; }

    [SavedProperty] public int AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.Creature?.CombatState != null;

    public override int DisplayAmount => (AstralParty_PersonSamuraiPrawnCounter - 1) % 3 + 1;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSamuraiPrawnCounter = 1;
        AstralParty_PersonSamuraiPrawnOpenedThisCombat = false;
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura = 0;
        InvokeDisplayAmountChanged();
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonSamuraiPrawnOpenedThisCombat = false;
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        await Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner) return;
        if (Owner?.Creature?.CombatState == null) return;

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;

        if ((AstralParty_PersonSamuraiPrawnCounter - 1) % 3 == 0)
        {
            if (AstralParty_PersonSamuraiPrawnOpenedThisCombat) return;

            Flash();
            var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFamousBlade>(), Owner);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
            AstralParty_PersonSamuraiPrawnOpenedThisCombat = true;
        }

        await Task.CompletedTask;
    }

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null) return;
        if (AstralParty_PersonSamuraiPrawnFirstAttackTriggered) return;
        if (command.Attacker != Owner.Creature) return;
        if (command.TargetSide == Owner.Creature.Side) return;

        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = true;

        if (Owner.Creature.GetPowerAmount<SwordAuraPower>() >= 3) return;

        Flash();

        await PowerCmd.Apply(
            ModelDb.Power<SwordAuraPower>().ToMutable(),
            Owner.Creature,
            1,
            Owner.Creature,
            null,
            false);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature?.CombatState == null) return;
        if (side != Owner.Creature.Side) return;

        AstralParty_PersonSamuraiPrawnCounter++;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonSamuraiPrawnOpenedThisCombat = false;
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        AstralParty_PersonSamuraiPrawnCounter++;
        InvokeDisplayAmountChanged();
        await Task.CompletedTask;
    }

    public int GetFamousBladeDamage()
    {
        return 1 + AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura / 2;
    }

    public void IncreaseFamousBladeConsumedAura(int amount)
    {
        if (amount <= 0) return;
        AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura += amount;
    }
}
