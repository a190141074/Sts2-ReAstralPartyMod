using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEtheriumAxe : EnigmaticSynthesisEtheriumWeaponRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_etherium_axe";

    protected override IEnumerable<IHoverTip> CreateCardHoverTips()
    {
        return [HoverTipFactory.FromCard<EnigmaticStrikeEtheriumAxe>()];
    }
}
