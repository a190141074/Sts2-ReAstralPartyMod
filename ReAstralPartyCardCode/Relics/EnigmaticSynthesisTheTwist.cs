using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisTheTwist : EnigmaticNonStackableUniqueMaterialRelicBase
{
    protected override string RelicId => "enigmatic_synthesis_the_twist";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await EnigmaticAcknowledgmentDeckHelper.ReplaceAcknowledgmentWithTwist(Owner);
    }

    public static Task<IReadOnlyList<EnigmaticSynthesisTheTwist>> GrantCopies(Player owner, int amount)
    {
        return EnigmaticNonStackableUniqueMaterialRelicBase.GrantCopies<EnigmaticSynthesisTheTwist>(owner, amount);
    }
}
