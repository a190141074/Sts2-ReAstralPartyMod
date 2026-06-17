using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class DeathRewindPower : AstralPartyPowerModel
{
    public override PowerType Type => (PowerType)1;

    public override PowerStackType StackType => (PowerStackType)2;

    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new("HealPercent", 0m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>()
    ];

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SyncHealPercent();
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is WithPower && power.Owner == Owner)
            SyncHealPercent();
        await Task.CompletedTask;
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature != Owner || Amount < 1)
            return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        Flash();
        var healPercent = Math.Min(100m, creature.GetPowerAmount<WithPower>());
        await PowerCmd.Remove<DeathRewindPower>(creature);
        var healAmount = Math.Max(1m, creature.MaxHp * (healPercent / 100m));
        await CreatureCmd.Heal(creature, healAmount);
    }

    private void SyncHealPercent()
    {
        if (Owner != null)
            DynamicVars["HealPercent"].BaseValue = Math.Min(100m, Owner.GetPowerAmount<WithPower>());
    }
}
