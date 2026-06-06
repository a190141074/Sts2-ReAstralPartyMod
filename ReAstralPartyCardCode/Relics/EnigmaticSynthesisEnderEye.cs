using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisEnderEye : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisEnderEyeStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisEnderEyeStacks;
        set => AstralParty_EnigmaticSynthesisEnderEyeStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesisender_eye";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticSynthesisEnderEye?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisEnderEye>(owner, amount);
    }
}
