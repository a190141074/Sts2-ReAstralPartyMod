using System;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "person_katana_master_doppelganger")]
public class PersonKatanaMasterDoppelganger : LegacyCooldownPersonRelicBase
{
    [SavedProperty]
    public int AstralParty_PersonKatanaMasterDoppelgangerCounter
    {
        get => GetCanonicalCounter();
        set => SetCanonicalCounter(value);
    }

    [SavedProperty]
    public bool AstralParty_PersonKatanaMasterDoppelgangerPendingCombatStartCard
    {
        get => GetCanonicalPendingCombatStartCard();
        set => SetCanonicalPendingCombatStartCard(value);
    }

    [SavedProperty] public bool AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered { get; set; }

    // Keep the removed legacy save field so older runs can deserialize after Famous Blade growth
    // moved entirely onto PersonalityDerivativeSwordIntent.
    public int AstralParty_PersonKatanaMasterDoppelgangerFamousBladeConsumedSwordGust
    {
        get => default;
        set { }
    }

    // Preserve the earliest Samurai Prawn save field so older runs can migrate into the new cooldown model.
    public bool AstralParty_PersonKatanaMasterDoppelgangerOpenedThisCombat
    {
        get => default;
        set => SetLegacyPendingAliasIfMissing(!value);
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
        HoverTipFactory.FromCard<SkillFamousBladeDoppelganger>(),
        HoverTipFactory.FromPower<SwordGustPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered = false;

        await PersonMultiplayerEffectHelper
            .ObtainDerivativeRelicIfMissing<PersonalityDerivativeSwordIntentDoppelganger>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered = false;
        await EnsureHandCostCapPower();
    }

    protected override async Task BeforeCooldownCardCheck(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player
    )
    {
        await EnsureSwordIntentRelic();
        await EnsureHandCostCapPower();
        AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered = false;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return;

        if (AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered)
            return;

        if (dealer != Owner.Creature || target.Side == Owner.Creature.Side || result.TotalDamage <= 0m)
            return;
        if (cardSource?.Owner != Owner || !WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return;

        AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered = true;

        if (Owner.Creature.GetPowerAmount<SwordGustPower>() >= 3)
            return;

        Flash();
        await PowerCmd.Apply(
            ModelDb.Power<SwordGustPower>().ToMutable(),
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
        AstralParty_PersonKatanaMasterDoppelgangerFirstAttackTriggered = false;
        return Task.CompletedTask;
    }

    protected override async Task GrantCooldownCard()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillFamousBladeDoppelganger>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true);
    }

    private async Task EnsureSwordIntentRelic()
    {
        if (Owner == null || Owner.GetRelic<PersonalityDerivativeSwordIntentDoppelganger>() != null)
            return;

        await PersonMultiplayerEffectHelper
            .ObtainDerivativeRelicIfMissing<PersonalityDerivativeSwordIntentDoppelganger>(Owner);
    }

    private async Task EnsureHandCostCapPower()
    {
        if (Owner?.Creature == null || Owner.Creature.HasPower<SamuraiPrawnHandCostCapPower>())
            return;

        await PowerCmd.Apply(
            ModelDb.Power<SamuraiPrawnHandCostCapPower>().ToMutable(),
            Owner.Creature,
            1m,
            Owner.Creature,
            null,
            false);
    }
}
