using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticHeartOfTheSea : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticHeartOfTheSeaStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticHeartOfTheSeaStacks;
        set => AstralParty_EnigmaticHeartOfTheSeaStacks = value;
    }

    protected override string RelicId => "enigmatic_heart_of_the_sea";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticHeartOfTheSea?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticHeartOfTheSea>(owner, amount);
    }
}
