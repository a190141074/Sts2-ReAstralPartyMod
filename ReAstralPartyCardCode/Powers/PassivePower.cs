using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class PassivePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max(0, (int)Amount);
}
