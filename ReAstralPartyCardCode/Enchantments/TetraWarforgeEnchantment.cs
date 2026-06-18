using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class TetraWarforgeEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/tetra_warforge_enchantment.png"
    };

    public override bool HasExtraCardText => true;

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && card.Type is CardType.Attack or CardType.Skill;
    }
}
