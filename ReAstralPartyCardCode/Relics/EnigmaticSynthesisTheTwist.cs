using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisTheTwist : AstralPartyRelicModel
{
    protected override string RelicId => "enigmatic_synthesis_the_twist";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EnigmaticAcknowledgmentDeckHelper.ReplaceAcknowledgmentWithTwist(Owner);
    }
}
