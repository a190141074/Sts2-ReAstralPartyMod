using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class AstralCooldownEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/astral_cooldown_enchantment.png"
    };

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && CanEnchantCardType(card.Type)
               && !card.Keywords.Contains(CardKeyword.Retain)
               && !card.Keywords.Contains(CardKeyword.Exhaust);
    }

    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Retain);
        Card.AddKeyword(CardKeyword.Exhaust);
    }
}
