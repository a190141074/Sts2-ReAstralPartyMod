using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticNefariousEssence : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticNefariousEssenceStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticNefariousEssenceStacks;
        set => AstralParty_EnigmaticNefariousEssenceStacks = value;
    }

    protected override string RelicId => "enigmatic_nefarious_essence";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticNefariousEssence?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticNefariousEssence>(owner, amount);
    }
}
