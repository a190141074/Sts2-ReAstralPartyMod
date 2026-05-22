using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class EssenceSacredFaithEnchantment : ModEnchantmentTemplate
{
    [SavedProperty] public decimal AstralParty_SacredFaithPermanentDamagePercent { get; set; }

    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/essence_sacred_faith_enchantment.png"
    };
}
