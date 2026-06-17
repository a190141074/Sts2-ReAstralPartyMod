using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class ThundersBreathPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => (int)Amount;
}
