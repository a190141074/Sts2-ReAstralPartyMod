using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
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

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonSamuraiPrawn : LegacyCooldownPersonaRelicBase
{
    [SavedProperty]
    public int AstralParty_PersonSamuraiPrawnCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonSamuraiPrawnPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    [SavedProperty] public bool AstralParty_PersonSamuraiPrawnFirstAttackTriggered { get; set; }

    // Keep the removed legacy save field name so older runs can deserialize after Famous Blade growth
    // moved entirely onto PersonalityDerivativeSwordIntent.
    public int AstralParty_PersonSamuraiPrawnFamousBladeConsumedAura
    {
        get => default;
        set { }
    }

    // Preserve the earliest Samurai Prawn save field so older runs can migrate into the new cooldown model.
    public bool AstralParty_PersonSamuraiPrawnOpenedThisCombat
    {
        get => default;
        set
        {
            SetLegacyPendingAliasIfMissing(!value);
        }
    }

    // Keep the removed legacy flag name so old saves can still deserialize without polluting SavedProperty sync.
    public new bool IsMelted
    {
        get => default;
        set { }
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillFamousBlade>(),
        HoverTipFactory.FromPower<SwordAuraPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeSwordIntent>(Owner);
    }

    protected override async Task BeforeCooldownCardCheck(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        await EnsureSwordIntentRelic();
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
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

    protected override Task AfterAdvanceCounterOnTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        return Task.CompletedTask;
    }

    protected override Task BeforeAdvanceCounterAfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonSamuraiPrawnFirstAttackTriggered = false;
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFamousBlade>(), Owner);
        await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private async Task EnsureSwordIntentRelic()
    {
        if (Owner == null || Owner.GetRelic<PersonalityDerivativeSwordIntent>() != null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeSwordIntent>(Owner);
    }
}
