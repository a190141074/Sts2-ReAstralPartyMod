using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticNetherStar : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticNetherStarStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticNetherStarStacks;
        set => AstralParty_EnigmaticNetherStarStacks = value;
    }

    protected override string RelicId => "enigmatic_nether_star";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticNetherStar?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticNetherStar>(owner, amount);
    }
}
