using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSynthesisRecallPotion : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticSynthesisRecallPotionStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticSynthesisRecallPotionStacks;
        set => AstralParty_EnigmaticSynthesisRecallPotionStacks = value;
    }

    protected override string RelicId => "enigmatic_synthesis_recall_potion";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticSynthesisRecallPotion?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticSynthesisRecallPotion>(owner, amount);
    }
}
