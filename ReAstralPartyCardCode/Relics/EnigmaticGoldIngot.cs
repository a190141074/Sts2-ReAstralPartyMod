using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticGoldIngot : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticGoldIngotStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticGoldIngotStacks;
        set => AstralParty_EnigmaticGoldIngotStacks = value;
    }

    protected override string RelicId => "enigmatic_gold_ingot";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticGoldIngot?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticGoldIngot>(owner, amount);
    }
}
