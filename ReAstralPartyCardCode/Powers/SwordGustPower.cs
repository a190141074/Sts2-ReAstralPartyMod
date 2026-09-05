using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using System.Collections.Generic;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class SwordGustPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return "res://ReAstralPartyMod/images/powers/sword_aura_power.png";
        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner || target == null || target.Side == Owner.Side)
            return 0m;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardSource))
            return 0m;

        return Amount switch
        {
            >= 3 => 2m,
            >= 1 => 1m,
            _ => 0m
        };
    }
}
