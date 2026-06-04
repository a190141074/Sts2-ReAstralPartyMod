using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticPhantomMembrane : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticPhantomMembraneStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticPhantomMembraneStacks;
        set => AstralParty_EnigmaticPhantomMembraneStacks = value;
    }

    protected override string RelicId => "enigmatic_phantom_membrane";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticPhantomMembrane?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticPhantomMembrane>(owner, amount);
    }
}
