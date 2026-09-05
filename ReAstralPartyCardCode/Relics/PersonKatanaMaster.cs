using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool), StableEntryStem = "person_katana_master")]
public sealed class PersonKatanaMaster : CooldownPersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonKatanaMasterCounter { get; set; } = 1;
    [SavedProperty] public bool AstralParty_PersonKatanaMasterPendingCombatStartCard { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterFirstAttackTriggered { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterTookDamageThisTurn { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterTurnStarted { get; set; }

    protected override int CounterValue
    {
        get => AstralParty_PersonKatanaMasterCounter;
        set => AstralParty_PersonKatanaMasterCounter = value;
    }

    protected override bool PendingCombatStartCard
    {
        get => AstralParty_PersonKatanaMasterPendingCombatStartCard;
        set => AstralParty_PersonKatanaMasterPendingCombatStartCard = value;
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;
    protected override int BaseMaxCounter => 3;
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillSakuraSetsudan>(),
        HoverTipFactory.FromCard<SkillTokenLegendarySwordGawuCut>(),
        .. HoverTipFactory.FromRelic<PersonalityDerivativeSwordGust>(),
        HoverTipFactory.FromPower<SwordGustPower>()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await PersonMultiplayerEffectHelper.ObtainDerivativeRelicIfMissing<PersonalityDerivativeSwordGust>(Owner);
    }

    public override async Task BeforeCombatStart()
    {
        AstralParty_PersonKatanaMasterFirstAttackTriggered = false;
        AstralParty_PersonKatanaMasterTookDamageThisTurn = false;
        AstralParty_PersonKatanaMasterTurnStarted = false;
        await EnsureHandCostPower();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return;

        // The first own turn has no preceding in-combat interval to evaluate;
        // the no-damage Sword Gust reward starts from the next own turn.
        if (AstralParty_PersonKatanaMasterTurnStarted && !AstralParty_PersonKatanaMasterTookDamageThisTurn)
            await GainSwordGust(1m);

        AstralParty_PersonKatanaMasterTurnStarted = true;
        AstralParty_PersonKatanaMasterFirstAttackTriggered = false;
        AstralParty_PersonKatanaMasterTookDamageThisTurn = false;
    }

    public override async Task BeforeAttack(AttackCommand command)
    {
        if (Owner?.Creature == null || AstralParty_PersonKatanaMasterFirstAttackTriggered)
            return;
        if (command.Attacker != Owner.Creature || command.TargetSide == Owner.Creature.Side)
            return;

        AstralParty_PersonKatanaMasterFirstAttackTriggered = true;
        await GainSwordGust(1m);
    }

    public override Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature != null && target == Owner.Creature && result.UnblockedDamage > 0m)
            AstralParty_PersonKatanaMasterTookDamageThisTurn = true;
        return Task.CompletedTask;
    }

    private async Task EnsureHandCostPower()
    {
        if (Owner?.Creature == null || Owner.Creature.HasPower<KatanaMasterHandCostPower>())
            return;
        await PowerCmd.Apply<KatanaMasterHandCostPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

    private async Task GainSwordGust(decimal amount)
    {
        if (Owner?.Creature == null || Owner.Creature.GetPowerAmount<SwordGustPower>() >= 3m)
            return;
        await PowerCmd.Apply<SwordGustPower>(Owner.Creature, Math.Min(amount, 3m - Owner.Creature.GetPowerAmount<SwordGustPower>()),
            Owner.Creature, null, false);
    }

    private async Task AddSakuraToHand()
    {
        if (Owner?.Creature?.CombatState == null)
            return;
        Flash();
        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillSakuraSetsudan>(), Owner);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(card, true, CardPilePosition.Top, this);
    }

    protected override Task GrantCooldownCard() => AddSakuraToHand();
}
