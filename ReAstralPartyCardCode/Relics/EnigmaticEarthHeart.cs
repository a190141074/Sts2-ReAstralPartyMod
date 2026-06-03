using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEarthHeart : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEarthHeartStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEarthHeartStacks;
        set => AstralParty_EnigmaticEarthHeartStacks = value;
    }

    protected override string RelicId => "enigmatic_earth_heart";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticEarthHeart?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEarthHeart>(owner, amount);
    }
}
