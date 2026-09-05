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
public sealed class PersonKatanaMaster : PersonRelicBase
{
    [SavedProperty] public int AstralParty_PersonKatanaMasterTurnCount { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterFirstAttackTriggered { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterTookDamageThisTurn { get; set; }
    [SavedProperty] public bool AstralParty_PersonKatanaMasterTurnStarted { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;
    public override bool ShouldReceiveCombatHooks => true;

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
        AstralParty_PersonKatanaMasterTurnCount = 0;
        AstralParty_PersonKatanaMasterFirstAttackTriggered = false;
        AstralParty_PersonKatanaMasterTookDamageThisTurn = false;
        AstralParty_PersonKatanaMasterTurnStarted = false;
        await EnsureHandCostPower();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner?.Creature == null || player != Owner)
            return;

        if (AstralParty_PersonKatanaMasterTurnStarted && !AstralParty_PersonKatanaMasterTookDamageThisTurn)
            await GainSwordGust(1m);

        AstralParty_PersonKatanaMasterTurnStarted = true;
        AstralParty_PersonKatanaMasterTurnCount++;
        AstralParty_PersonKatanaMasterFirstAttackTriggered = false;
        AstralParty_PersonKatanaMasterTookDamageThisTurn = false;

        if ((AstralParty_PersonKatanaMasterTurnCount - 1) % 3 == 0)
            await AddSakuraToHand();
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
}
