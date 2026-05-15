using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class ArtKnifeRelicBase : AstralPartyRelicModel
{
    private bool _strengthApplied;

    protected abstract decimal StrengthBonus { get; }
    protected abstract CardType DamageCardType { get; }
    protected virtual decimal HealDamageDivisor => 1m;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public override async Task BeforeCombatStart()
    {
        await SyncStrengthState();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner)
            await SyncStrengthState();
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner?.Creature != null && side == Owner.Creature.Side)
            await SyncStrengthState();
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target == Owner?.Creature)
            await SyncStrengthState();
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner?.Creature)
            await SyncStrengthState();
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner)
            await SyncStrengthState();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _strengthApplied = false;
        return Task.CompletedTask;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (!IsAtFullHp())
            return 0m;
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (cardSource?.Type != DamageCardType)
            return 0m;

        return Owner.Creature.GetPowerAmount<HalfLifeHealPower>() / HealDamageDivisor;
    }

    private bool IsAtFullHp()
    {
        return Owner?.Creature != null
               && Owner.Creature.MaxHp > 0m
               && Owner.Creature.CurrentHp >= Owner.Creature.MaxHp;
    }

    private async Task SyncStrengthState()
    {
        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
            return;

        var shouldApply = IsAtFullHp();
        if (shouldApply == _strengthApplied)
            return;

        _strengthApplied = shouldApply;
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            shouldApply ? StrengthBonus : -StrengthBonus,
            Owner.Creature,
            null,
            true);
    }
}
