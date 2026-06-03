using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticEnderEye : EnigmaticUniqueMaterialRelicBase
{
    [SavedProperty] public int AstralParty_EnigmaticEnderEyeStacks { get; set; } = 1;

    protected override int StoredStacks
    {
        get => AstralParty_EnigmaticEnderEyeStacks;
        set => AstralParty_EnigmaticEnderEyeStacks = value;
    }

    protected override string RelicId => "enigmatic_ender_eye";

    public override RelicRarity Rarity => RelicRarity.Rare;

    public static Task<EnigmaticEnderEye?> GrantStacks(Player owner, int amount)
    {
        return EnigmaticUniqueMaterialRelicBase.GrantStacks<EnigmaticEnderEye>(owner, amount);
    }
}
