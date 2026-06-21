using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
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

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && CanEnchantCardType(card.Type);
    }
}
