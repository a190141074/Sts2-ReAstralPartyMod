using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticDarkestScroll : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticDarkestScrollStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticDarkestScrollStacks;
        set => AstralParty_EnigmaticDarkestScrollStacks = value;
    }

    protected override string RelicId => "enigmatic_darkest_scroll";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticDarkestScroll?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticDarkestScroll>(owner, amount);
    }
}
