using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class SwordGustPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return "res://ReAstralPartyMod/images/powers/sword_gust_power.png";
        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (Owner?.Player != null)
            await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(Owner.Player, cardSource);
    }

    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (power == this && Owner?.Player != null)
            await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(Owner.Player, cardSource);
    }

    public override async Task AfterRemoved(Creature? oldOwner)
    {
        if (oldOwner?.Player != null)
            await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(oldOwner.Player);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner || target == null || target.Side == Owner.Side)
            return 0m;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return 0m;

        if (Amount >= 3m)
            return 5m;
        if (Amount >= 2m)
            return 2m;
        if (Amount >= 1m)
            return 1m;
        return 0m;
    }
}
