using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class AbyssalTentaclesPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    public override decimal ModifyHpLostBeforeOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        return IsTentacleEntity && target == Owner && amount > 0m ? 0m : amount;
    }

    public override bool ShouldAllowTargeting(Creature target)
    {
        return !IsTentacleEntity || target != Owner;
    }

    private bool IsTentacleEntity => Owner?.Monster is AbyssalTentacleMonster;
}
