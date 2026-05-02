using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

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

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature == Owner?.Creature)
            await SyncStrengthState();
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (Owner?.Creature != null && power.Owner == Owner.Creature && power is BloodthirstPower)
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
        if (Owner.Creature.MaxHp <= 0)
            return 0m;

        if (Owner.Creature.HasPower<BloodthirstPower>())
            return QuarterHpBonus;

        if (LowHpStateHelper.IsBelowQuarterHp(Owner.Creature))
            return QuarterHpBonus;
        if (LowHpStateHelper.IsBelowHalfHp(Owner.Creature))
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
