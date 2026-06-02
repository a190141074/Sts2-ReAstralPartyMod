using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class CursedScrollDeckHelper
{
    private const decimal AttackDamageBonusPerWeightedCurse = 0.06m;
    private const decimal GoldGainBonusPerWeightedCurse = 0.14m;
    private const decimal HealGainBonusPerWeightedCurse = 0.08m;
    private const int ExtraDrawDivisor = 3;

    public static int GetWeightedCurseCount(Player? owner)
    {
        if (owner == null)
            return 0;

        var weightedCount = 0;
        foreach (var card in EventDeckCardHelper.GetRunDeckCards(owner))
        {
            if (card.Type != CardType.Curse)
                continue;

            weightedCount += card.Keywords.Contains(CardKeyword.Eternal) ? 2 : 1;
        }

        return weightedCount;
    }

    public static decimal GetAttackDamageBonus(decimal amount, int weightedCurseCount)
    {
        return amount <= 0m || weightedCurseCount <= 0
            ? 0m
            : amount * weightedCurseCount * AttackDamageBonusPerWeightedCurse;
    }

    public static decimal GetGoldGainBonus(decimal amount, int weightedCurseCount)
    {
        return amount <= 0m || weightedCurseCount <= 0
            ? 0m
            : amount * weightedCurseCount * GoldGainBonusPerWeightedCurse;
    }

    public static decimal GetHealGainBonus(decimal amount, int weightedCurseCount)
    {
        return amount <= 0m || weightedCurseCount <= 0
            ? 0m
            : amount * weightedCurseCount * HealGainBonusPerWeightedCurse;
    }

    public static int GetExtraDrawCount(int weightedCurseCount)
    {
        return weightedCurseCount <= 0 ? 0 : weightedCurseCount / ExtraDrawDivisor;
    }
}
