using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class ParanoidEssenceEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/paranoid_essence_enchantment.png"
    };
}
