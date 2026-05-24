using MegaCrit.Sts2.Core.Entities.Creatures;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class EyeForAnEyePower : BaseAbilityRetaliationPowerBase
{
    protected override Creature? ResolveRetaliationTarget(Creature source)
    {
        return source;
    }
}
