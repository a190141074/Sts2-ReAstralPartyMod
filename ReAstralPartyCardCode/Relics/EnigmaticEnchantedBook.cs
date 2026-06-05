using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEnchantedBook : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEnchantedBookStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEnchantedBookStacks;
        set => AstralParty_EnigmaticEnchantedBookStacks = value;
    }

    protected override string RelicId => "enigmatic_enchanted_book";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticEnchantedBook?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEnchantedBook>(owner, amount);
    }
}
