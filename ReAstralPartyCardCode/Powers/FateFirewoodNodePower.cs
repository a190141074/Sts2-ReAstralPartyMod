using MegaCrit.Sts2.Core.Entities.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower]
public sealed class FateFirewoodNodePower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max((int)Amount, 0);

    protected override IEnumerable<string> GetCandidateIconPaths()
    {
        yield return "res://ReAstralPartyMod/images/powers/fate_firewood_node_power.png";
        yield return "res://ReAstralPartyMod/images/powers/moses_node_power.png";

        foreach (var path in base.GetCandidateIconPaths())
            yield return path;
    }
}
