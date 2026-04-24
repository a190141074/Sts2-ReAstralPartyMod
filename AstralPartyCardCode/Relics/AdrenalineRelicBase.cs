using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
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

namespace AstralPartyMod.AstralPartyCardCode.Relics;

public abstract class AdrenalineRelicBase : AstralPartyRelicModel
{
    private decimal _currentStrengthBonus;

    protected abstract decimal HalfHpBonus { get; }
    protected abstract decimal QuarterHpBonus { get; }

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
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

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner == Owner)
            await SyncStrengthState();
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (dealer == Owner?.Creature)
            await SyncStrengthState();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _currentStrengthBonus = 0m;
        return Task.CompletedTask;
    }

    private decimal GetDesiredStrengthBonus()
    {
        if (Owner?.Creature == null)
            return 0m;
        if (Owner.Creature.MaxHp <= 0m)
            return 0m;

        var hpRatio = Owner.Creature.CurrentHp / Owner.Creature.MaxHp;
        if (hpRatio < 0.25m)
            return QuarterHpBonus;
        if (hpRatio < 0.5m)
            return HalfHpBonus;

        return 0m;
    }

    private async Task SyncStrengthState()
    {
        if (Owner?.Creature == null || Owner.Creature.CombatState == null)
            return;

        var desiredBonus = GetDesiredStrengthBonus();
        var delta = desiredBonus - _currentStrengthBonus;
        if (delta == 0m)
            return;

        _currentStrengthBonus = desiredBonus;
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            Owner.Creature,
            delta,
            Owner.Creature,
            null,
            true);
    }
}
