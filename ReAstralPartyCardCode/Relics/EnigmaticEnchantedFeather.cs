using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEnchantedFeather : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEnchantedFeatherStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEnchantedFeatherStacks;
        set => AstralParty_EnigmaticEnchantedFeatherStacks = value;
    }

    protected override string RelicId => "enigmatic_enchanted_feather";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticEnchantedFeather?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEnchantedFeather>(owner, amount);
    }
}
