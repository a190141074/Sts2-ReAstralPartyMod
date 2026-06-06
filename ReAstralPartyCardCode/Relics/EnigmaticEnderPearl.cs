using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEnderPearl : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEnderPearlStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEnderPearlStacks;
        set => AstralParty_EnigmaticEnderPearlStacks = value;
    }

    protected override string RelicId => "enigmatic_ender_pearl";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticEnderPearl?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEnderPearl>(owner, amount);
    }
}
