using MegaCrit.Sts2.Core.Entities.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class ExposurePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;
}