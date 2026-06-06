using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEnderRod : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEnderRodStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisEnderRodStacks;
        set => AstralParty_EnigmaticSynthesisEnderRodStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_ender_rod";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticSynthesisEnderRod?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisEnderRod>(owner, amount);
    }
}
