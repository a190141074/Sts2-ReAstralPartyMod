using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEmerald : EnigmaticStackableUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEmeraldStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEmeraldStacks;
        set => AstralParty_EnigmaticEmeraldStacks = value;
    }

    protected override string RelicId => "enigmatic_emerald";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticEmerald?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEmerald>(owner, amount);
    }
}
