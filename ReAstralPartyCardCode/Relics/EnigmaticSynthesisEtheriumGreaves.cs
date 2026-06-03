using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEtheriumGreaves : EnigmaticSynthesisEtheriumArmorRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_etherium_greaves";

    protected override decimal FullPlatingAmount => 4m;

    protected override decimal FullRegenAmount => 1m;
}
