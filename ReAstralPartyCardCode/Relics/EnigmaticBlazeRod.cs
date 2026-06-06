using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticBlazeRod : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticBlazeRodStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticBlazeRodStacks;
        set => AstralParty_EnigmaticBlazeRodStacks = value;
    }

    protected override string RelicId => "enigmatic_blaze_rod";

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public static Task<EnigmaticBlazeRod?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticBlazeRod>(owner, amount);
    }
}
