using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

public sealed class DisabledPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "colorless";

#pragma warning disable CS0618
    [Obsolete]
    protected override IEnumerable<Type> PotionTypes => [];
#pragma warning restore CS0618
}
