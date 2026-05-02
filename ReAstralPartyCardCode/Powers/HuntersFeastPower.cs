using System;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class HuntersFeastPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);
}