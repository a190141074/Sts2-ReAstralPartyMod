using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RelicPools;

public sealed class PvzNutSeriesRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            ModelDb.Relic<PvzRareHyperTemporalNut>(),
            ModelDb.Relic<PvzRareSunshineNut>(),
            ModelDb.Relic<PvzRareBigMouthedNut>(),
            ModelDb.Relic<PvzRareAngmaoNut>(),
            ModelDb.Relic<PvzUltimateHyperSpacetimeNut>(),
            ModelDb.Relic<PvzUltimateSunshineEmperorNut>()
        ];
    }
}
