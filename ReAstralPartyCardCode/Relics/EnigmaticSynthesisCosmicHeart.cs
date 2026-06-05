using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisCosmicHeart : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisCosmicHeartStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisCosmicHeartStacks;
        set => AstralParty_EnigmaticSynthesisCosmicHeartStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_cosmic_heart";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticSynthesisCosmicHeart?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisCosmicHeart>(owner, amount);
    }
}
