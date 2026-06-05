using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticBlazePowder : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticBlazePowderStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticBlazePowderStacks;
        set => AstralParty_EnigmaticBlazePowderStacks = value;
    }

    protected override string RelicId => "enigmatic_blaze_powder";

    public override RelicRarity Rarity => RelicRarity.Common;

    public static Task<EnigmaticBlazePowder?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticBlazePowder>(owner, amount);
    }
}
