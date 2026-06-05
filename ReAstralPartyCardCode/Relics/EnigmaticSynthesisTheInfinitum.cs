using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class EnigmaticSynthesisTheInfinitum : AstralPartyRelicModel
{
    protected override string RelicId => "enigmatic_synthesis_the_infinitum";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EnigmaticAcknowledgmentDeckHelper.ReplaceTwistWithInfinitum(Owner);
    }
}
