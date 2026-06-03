using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEtheriumHelmet : EnigmaticSynthesisEtheriumArmorRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_etherium_helmet";

    protected override decimal FullPlatingAmount => 2m;
}
