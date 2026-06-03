using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticRedstoneDust : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticRedstoneDustStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticRedstoneDustStacks;
        set => AstralParty_EnigmaticRedstoneDustStacks = value;
    }

    protected override string RelicId => "enigmatic_redstone_dust";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticRedstoneDust?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticRedstoneDust>(owner, amount);
    }
}
