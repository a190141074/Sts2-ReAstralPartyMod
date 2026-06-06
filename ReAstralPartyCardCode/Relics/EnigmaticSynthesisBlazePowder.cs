using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisBlazePowder : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisBlazePowderStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisBlazePowderStacks;
        set => AstralParty_EnigmaticSynthesisBlazePowderStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_blaze_powder";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticSynthesisBlazePowder?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisBlazePowder>(owner, amount);
    }
}
