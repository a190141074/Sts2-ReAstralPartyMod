using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Enchantments;

public class AstralCooldownEnchantment : CustomEnchantmentModel
{
    protected override string? CustomIconPath =>
        "res://AstralPartyMod/images/enchantments/astral_cooldown_enchantment.png";

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && !card.Keywords.Contains(CardKeyword.Retain)
               && !card.Keywords.Contains(CardKeyword.Exhaust);
    }

    protected override void OnEnchant()
    {
        Card.AddKeyword(CardKeyword.Retain);
        Card.AddKeyword(CardKeyword.Exhaust);
    }
}