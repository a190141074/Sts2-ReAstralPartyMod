using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEtheriumBoots : EnigmaticSynthesisEtheriumArmorRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_etherium_boots";

    protected override decimal FullPlatingAmount => 2m;
}
