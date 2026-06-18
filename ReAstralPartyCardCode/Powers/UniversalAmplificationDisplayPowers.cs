using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class AttackAmplificationDisplayPower : AstralPartyPowerModel
{
    private const string DisplayIconPath = "res://ReAstralPartyMod/images/temp_power/universal_amplification_power.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ATTACK_AMPLIFICATION_DISPLAY_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_ATTACK_AMPLIFICATION_DISPLAY_POWER.description");

    protected override string ResolveIconPath()
    {
        return DisplayIconPath;
    }
}

public sealed class SkillAmplificationDisplayPower : AstralPartyPowerModel
{
    private const string DisplayIconPath = "res://ReAstralPartyMod/images/temp_power/universal_amplification_power.png";

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    public override LocString Title =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_AMPLIFICATION_DISPLAY_POWER.title");

    public override LocString Description =>
        new("powers", "RE_ASTRAL_PARTY_MOD_POWER_SKILL_AMPLIFICATION_DISPLAY_POWER.description");

    protected override string ResolveIconPath()
    {
        return DisplayIconPath;
    }
}
