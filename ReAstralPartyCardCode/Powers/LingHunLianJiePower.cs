using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

/// <summary>
/// 灵魂联结 - 灵魂联结能力（具体效果待实现�?/// </summary>
public class LingHunLianJiePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;
}