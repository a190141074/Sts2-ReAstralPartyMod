using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class FanPower : AstralPartyPowerModel
{
    private const decimal BaseDamagePenalty = -1m;
    private const decimal IdolTargetAdditionalPenalty = -1m;

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return 0m;
        if (target == null || target.Side == Owner.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;

        var penalty = BaseDamagePenalty;
        if (KawaiiPersonaHelper.IsIdolTarget(target))
            penalty += IdolTargetAdditionalPenalty;

        return penalty;
    }
}
