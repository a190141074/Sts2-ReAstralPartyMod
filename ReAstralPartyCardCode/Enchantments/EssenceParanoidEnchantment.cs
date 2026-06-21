using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class EssenceParanoidEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/essence_paranoid_enchantment.png"
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
