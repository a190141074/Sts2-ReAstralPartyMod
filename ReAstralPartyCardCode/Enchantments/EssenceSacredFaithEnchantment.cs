using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public class EssenceSacredFaithEnchantment : ModEnchantmentTemplate
{
    [SavedProperty] public int AstralParty_SacredFaithPermanentDamagePercentTenths { get; set; }

    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/essence_sacred_faith_enchantment.png"
    };

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType == CardType.Attack;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card)
               && CanEnchantCardType(card.Type);
    }
}
