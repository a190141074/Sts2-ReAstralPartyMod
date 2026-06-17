using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class SettingSunPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.None;

    public override int DisplayAmount => (int)Amount;
}
