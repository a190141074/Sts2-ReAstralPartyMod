using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RelicPools;

public sealed class DiceSeriesRelicPool : RelicPoolModel
{
    public override string EnergyColorName => "colorless";

    protected override IEnumerable<RelicModel> GenerateAllRelics()
    {
        return
        [
            ModelDb.Relic<TokenBlueDie4>(),
            ModelDb.Relic<TokenBlueDie6>(),
            ModelDb.Relic<TokenBlueDie10>(),
            ModelDb.Relic<TokenBlueDie12>(),
            ModelDb.Relic<TokenBlueDie20>()
        ];
    }
}
