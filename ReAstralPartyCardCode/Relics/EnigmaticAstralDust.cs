using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticAstralDust : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticAstralDustStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticAstralDustStacks;
        set => AstralParty_EnigmaticAstralDustStacks = value;
    }

    protected override string RelicId => "enigmatic_astral_dust";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticAstralDust?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticAstralDust>(owner, amount);
    }
}
