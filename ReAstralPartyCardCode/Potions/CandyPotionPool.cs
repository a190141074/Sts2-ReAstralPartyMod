using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

public class CandyPotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "colorless";

#pragma warning disable CS0618
    [Obsolete]
    protected override IEnumerable<Type> PotionTypes =>
    [
        typeof(CandySupportGum),
        typeof(CandyEnergySupplementBar),
        typeof(CandyBigBrainGummy)
    ];
#pragma warning restore CS0618
}