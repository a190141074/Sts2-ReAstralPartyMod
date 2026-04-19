using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.cards;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(EventRelicPool))]
public class PersonalityDerivativeXiaoLeiDragonGate : AstralPartyRelicModel
{
    [SavedProperty] public int AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter { get; set; }

    [SavedProperty]
    public bool AstralParty_PersonalityDerivativeXiaoLeiDragonGatePendingTurnStartEffect { get; set; } = true;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => true;

    public override int DisplayAmount => AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromCard<SkillDragonsRoar>(),
        HoverTipFactory.FromPower<DragonAwakeningPower>(),
        HoverTipFactory.FromPower<TrueDragonFormPower>(),
        BuildProgressHoverTip()
    ];

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter = 72;
        AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter = 36;
        AstralParty_PersonalityDerivativeXiaoLeiDragonGatePendingTurnStartEffect = true;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;
        if (!AstralParty_PersonalityDerivativeXiaoLeiDragonGatePendingTurnStartEffect || !IsUnlocked)
            return;

        AstralParty_PersonalityDerivativeXiaoLeiDragonGatePendingTurnStartEffect = false;

        if (Owner.Creature.GetPower<TrueDragonFormPower>() != null)
            return;

        Flash();
        await GrantDragonsRoar();
        await PowerCmd.Apply<TrueDragonFormPower>(Owner.Creature, 1m, Owner.Creature, null, false);
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return Task.CompletedTask;
        if (result.UnblockedDamage <= 0)
            return Task.CompletedTask;
        if (AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter <= 0)
            return Task.CompletedTask;

        AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter = Math.Max(
            0,
            AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter - 1);
        Flash();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner?.Creature == null)
            return Task.CompletedTask;

        AstralParty_PersonalityDerivativeXiaoLeiDragonGatePendingTurnStartEffect = true;

        var awakeningAmount = (int)Owner.Creature.GetPowerAmount<DragonAwakeningPower>();
        if (awakeningAmount <= 0 || AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter <= 0)
            return Task.CompletedTask;

        AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter = Math.Max(
            0,
            AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter - awakeningAmount);
        Flash();
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private bool IsUnlocked =>
        AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter <= 0
        && AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter <= 0;

    private async Task GrantDragonsRoar()
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var card = Owner.Creature.CombatState.CreateCard(ModelDb.Card<SkillDragonsRoar>(), Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
    }

    private HoverTip BuildProgressHoverTip()
    {
        var title = new LocString(
            "relics",
            "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_XIAO_LEI_DRAGON_GATE.progress_title");
        var template = new LocString(
            "relics",
            "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_XIAO_LEI_DRAGON_GATE.progress_description").GetRawText();
        var unlockedText = new LocString(
            "relics",
            IsUnlocked
                ? "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_XIAO_LEI_DRAGON_GATE.unlocked"
                : "ASTRALPARTYMOD-PERSONALITY_DERIVATIVE_XIAO_LEI_DRAGON_GATE.locked").GetRawText();
        var body = string.Format(
            template,
            AstralParty_PersonalityDerivativeXiaoLeiDragonGateThunderCounter,
            AstralParty_PersonalityDerivativeXiaoLeiDragonGateFireCounter,
            unlockedText);
        return new HoverTip(title, body, GD.Load<Texture2D>(PackedIconPath));
    }
}