using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisTwistedHeart : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisTwistedHeartStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisTwistedHeartStacks;
        set => AstralParty_EnigmaticSynthesisTwistedHeartStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_twisted_heart";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticSynthesisTwistedHeart?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisTwistedHeart>(owner, amount);
    }
}
