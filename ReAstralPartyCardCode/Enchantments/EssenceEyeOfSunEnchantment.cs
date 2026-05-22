using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class EssenceEyeOfSunEnchantment : ModEnchantmentTemplate
{
    [SavedProperty] public int AstralParty_EyeOfSunPlayedCount { get; set; }
    [SavedProperty] public int AstralParty_EyeOfSunTriggerCount { get; set; }

    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/essence_eye_of_sun_enchantment.png"
    };
}
