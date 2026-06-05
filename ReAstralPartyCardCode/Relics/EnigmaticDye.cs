using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticDye : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticDyeStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticDyeStacks;
        set => AstralParty_EnigmaticDyeStacks = value;
    }

    protected override string RelicId => "enigmatic_dye";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticDye?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticDye>(owner, amount);
    }
}
