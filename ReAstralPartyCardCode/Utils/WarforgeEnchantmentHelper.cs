using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class WarforgeEnchantmentHelper
{
    public static bool HasWarforge(CardModel? card)
    {
        return card?.Enchantment is TetraWarforgeEnchantment;
    }

    public static bool CountsAsAttack(CardModel? card)
    {
        if (card == null)
            return false;

        return card.Type == CardType.Attack
               || (HasWarforge(card) && card.Type == CardType.Skill);
    }

    public static bool CountsAsSkill(CardModel? card)
    {
        if (card == null)
            return false;

        return card.Type == CardType.Skill
               || (HasWarforge(card) && card.Type == CardType.Attack);
    }

    public static bool MatchesCardType(CardModel? card, CardType type)
    {
        return type switch
        {
            CardType.Attack => CountsAsAttack(card),
            CardType.Skill => CountsAsSkill(card),
            _ => card?.Type == type
        };
    }
}
