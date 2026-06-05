using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class CursedScrollDeckHelper
{
    private const decimal AttackDamageBonusPerWeightedCurse = 0.06m;
    private const decimal GoldGainBonusPerWeightedCurse = 0.10m;
    private const decimal HealGainBonusPerWeightedCurse = 0.06m;
    private const decimal DamageReductionPerWeightedCurse = 0.02m;
    private const decimal MinimumDamageMultiplier = 0.30m;
    private const int ExtraDrawDivisor = 3;
    private const int SevenCursesBonusWeightedCount = 7;

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

        if (owner.GetRelic<EnigmaticSevenCurses>() != null)
            weightedCount += SevenCursesBonusWeightedCount;

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

    public static decimal GetDamageTakenMultiplier(int weightedCurseCount)
    {
        if (weightedCurseCount <= 0)
            return 1m;

        return Math.Max(MinimumDamageMultiplier, 1m - weightedCurseCount * DamageReductionPerWeightedCurse);
    }
}
