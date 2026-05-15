using System;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class CopyQuotaPower : AstralPartyPowerModel
{
    public const int MaxTrackedSkillsPerTurn = 10;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Store remaining uses internally, but show how many skill cards have been tracked this turn.
    public override int DisplayAmount =>
        Math.Clamp(MaxTrackedSkillsPerTurn - (int)Amount, 0, MaxTrackedSkillsPerTurn - 1);
}
