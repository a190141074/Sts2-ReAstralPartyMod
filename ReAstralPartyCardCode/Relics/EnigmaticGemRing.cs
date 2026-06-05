using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticGemRing : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticGemRingStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticGemRingStacks;
        set => AstralParty_EnigmaticGemRingStacks = value;
    }

    protected override string RelicId => "enigmatic_gem_ring";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticGemRing?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticGemRing>(owner, amount);
    }
}
