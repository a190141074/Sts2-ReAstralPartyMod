using System;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public class ShadowsLimitPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Store remaining uses plus one so the display can show 0 without the engine removing the power.
    public override int DisplayAmount => Math.Max(Amount - 1, 0);
}