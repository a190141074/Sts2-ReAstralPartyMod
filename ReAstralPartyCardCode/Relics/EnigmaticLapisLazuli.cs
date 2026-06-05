using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticLapisLazuli : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticLapisLazuliStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticLapisLazuliStacks;
        set => AstralParty_EnigmaticLapisLazuliStacks = value;
    }

    protected override string RelicId => "enigmatic_lapis_lazuli";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticLapisLazuli?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticLapisLazuli>(owner, amount);
    }
}
