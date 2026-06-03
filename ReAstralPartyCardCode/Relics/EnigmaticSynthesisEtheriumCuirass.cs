using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEtheriumCuirass : EnigmaticSynthesisEtheriumArmorRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_etherium_cuirass";

    protected override decimal FullPlatingAmount => 5m;

    protected override decimal FullRegenAmount => 1m;
}
