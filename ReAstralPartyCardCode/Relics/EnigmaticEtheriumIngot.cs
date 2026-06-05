using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEtheriumIngot : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEtheriumIngotStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEtheriumIngotStacks;
        set => AstralParty_EnigmaticEtheriumIngotStacks = value;
    }

    protected override string RelicId => "enigmatic_etherium_ingot";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static async Task<EnigmaticEtheriumIngot?> GrantStacks(Player owner, int amount)
    {
        return await EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEtheriumIngot>(owner, amount);
    }
}
