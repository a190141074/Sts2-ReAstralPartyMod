using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

public class VialEpisodePotionPool : TypeListPotionPoolModel
{
    public override string EnergyColorName => "colorless";

#pragma warning disable CS0618
    [Obsolete]
    protected override IEnumerable<Type> PotionTypes =>
    [
        typeof(VialAnomalyEventPotion),
        typeof(VialNeutralEventPotion),
        typeof(VialGoodEventPotion),
        typeof(VialDeusExMachinaPotion)
    ];
#pragma warning restore CS0618
}
