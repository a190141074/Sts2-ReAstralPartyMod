using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticNetheriteIngot : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticNetheriteIngotStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticNetheriteIngotStacks;
        set => AstralParty_EnigmaticNetheriteIngotStacks = value;
    }

    protected override string RelicId => "enigmatic_netherite_ingot";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticNetheriteIngot?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticNetheriteIngot>(owner, amount);
    }
}
