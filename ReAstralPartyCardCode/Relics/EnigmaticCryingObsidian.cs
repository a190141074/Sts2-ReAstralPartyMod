using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticCryingObsidian : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticCryingObsidianStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticCryingObsidianStacks;
        set => AstralParty_EnigmaticCryingObsidianStacks = value;
    }

    protected override string RelicId => "enigmatic_crying_obsidian";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticCryingObsidian?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticCryingObsidian>(owner, amount);
    }
}
