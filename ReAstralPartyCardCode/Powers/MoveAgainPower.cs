using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class MoveAgainPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => (int)Amount;

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_MOVE_AGAIN_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_MOVE_AGAIN_POWER.description");
}
