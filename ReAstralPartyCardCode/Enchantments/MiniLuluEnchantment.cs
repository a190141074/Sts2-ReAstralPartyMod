using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class MiniLuluEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/mini_lulu_enchantment.png"
    };

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill or CardType.Power;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && CanEnchantCardType(card.Type)
               && card.Enchantment == null;
    }
}
