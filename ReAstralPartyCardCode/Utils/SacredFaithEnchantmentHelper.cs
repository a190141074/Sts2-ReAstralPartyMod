using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SacredFaithEnchantmentHelper
{
    private const decimal NormalKillGrowthPercent = 1.5m;
    private const decimal EliteOrBossKillGrowthPercent = 6m;
    private const int PercentTenthsScale = 10;

    public static bool HasSacredFaith(CardModel? card)
    {
        return card?.Enchantment is EssenceSacredFaithEnchantment;
    }

    public static bool ShouldForceCombatHooks(CardModel? card)
    {
        return HasSacredFaith(card);
    }

    public static decimal GetDamageMultiplier(CardModel? card)
    {
        if (card?.Enchantment is not EssenceSacredFaithEnchantment enchantment)
            return 1m;

        return 1m + GetStoredPercent(enchantment) / 100m;
    }

    public static decimal GetDamageBonus(CardModel? card, decimal baseAmount)
    {
        if (baseAmount <= 0m)
            return 0m;
        if (card?.Enchantment is not EssenceSacredFaithEnchantment)
            return 0m;

        var multiplier = GetDamageMultiplier(card);
        return Math.Max(0m, baseAmount * multiplier - baseAmount);
    }

    public static void RegisterKill(CardModel? card, Creature target)
    {
        if (card?.Enchantment is not EssenceSacredFaithEnchantment enchantment)
            return;

        enchantment.AstralParty_SacredFaithPermanentDamagePercentTenths +=
            ConvertPercentToStoredValue(IsEliteOrBoss(target) ? EliteOrBossKillGrowthPercent : NormalKillGrowthPercent);

        TryRefreshDisplayedDamage(card);
    }

    public static void TryRefreshDisplayedDamage(CardModel? card)
    {
        if (card == null)
            return;
        if (!card.DynamicVars.ContainsKey("Damage"))
            return;

        var currentBase = card.DynamicVars["Damage"].BaseValue;
        if (currentBase <= 0m)
            return;

        var normalizedBase = currentBase / GetDamageMultiplier(card);
        card.DynamicVars["Damage"].BaseValue = normalizedBase * GetDamageMultiplier(card);
    }

    private static bool IsEliteOrBoss(Creature target)
    {
        var roomType = target.CombatState?.Encounter?.RoomType;
        return roomType is RoomType.Elite or RoomType.Boss;
    }

    private static decimal GetStoredPercent(EssenceSacredFaithEnchantment enchantment)
    {
        return enchantment.AstralParty_SacredFaithPermanentDamagePercentTenths / (decimal)PercentTenthsScale;
    }

    private static int ConvertPercentToStoredValue(decimal percent)
    {
        return (int)(percent * PercentTenthsScale);
    }
}
