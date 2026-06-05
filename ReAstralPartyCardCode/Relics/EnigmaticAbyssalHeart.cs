using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticAbyssalHeart : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticAbyssalHeartStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticAbyssalHeartStacks;
        set => AstralParty_EnigmaticAbyssalHeartStacks = value;
    }

    protected override string RelicId => "enigmatic_abyssal_heart";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticAbyssalHeart?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticAbyssalHeart>(owner, amount);
    }
}
