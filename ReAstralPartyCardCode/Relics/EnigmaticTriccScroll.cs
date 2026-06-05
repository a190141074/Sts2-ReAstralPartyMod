using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticTriccScroll : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticTriccScrollStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticTriccScrollStacks;
        set => AstralParty_EnigmaticTriccScrollStacks = value;
    }

    protected override string RelicId => "enigmatic_tricc_scroll";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticTriccScroll?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticTriccScroll>(owner, amount);
    }
}
