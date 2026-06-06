using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEvilIngot : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEvilIngotStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisEvilIngotStacks;
        set => AstralParty_EnigmaticSynthesisEvilIngotStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_evil_ingot";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticSynthesisEvilIngot?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisEvilIngot>(owner, amount);
    }
}
