using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticAwkwardPotion : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticAwkwardPotionStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticAwkwardPotionStacks;
        set => AstralParty_EnigmaticAwkwardPotionStacks = value;
    }

    protected override string RelicId => "enigmatic_awkward_potion";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticAwkwardPotion?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticAwkwardPotion>(owner, amount);
    }
}
