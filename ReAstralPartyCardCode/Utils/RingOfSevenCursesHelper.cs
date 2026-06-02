using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class RingOfSevenCursesHelper
{
    public const string SeriesId = "ring_of_seven_curses";
    private const int ExtraRewardCardOptionCount = 4;

    public static async Task EnsureRelicPairAsync<TMissing>(Player? owner)
        where TMissing : RelicModel
    {
        if (owner == null)
            return;
        if (owner.GetRelic<TMissing>() != null)
            return;

        var canonicalRelic = ModelDb.Relic<TMissing>().CanonicalInstance ?? ModelDb.Relic<TMissing>();
        if (PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(owner, canonicalRelic))
            return;

        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(owner, canonicalRelic);
    }

    public static bool RollPermille(
        int thresholdPermille,
        params object?[] contextParts)
    {
        if (thresholdPermille <= 0)
            return false;
        if (thresholdPermille >= 1000)
            return true;

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            1000,
            contextParts);
        return roll < thresholdPermille;
    }

    public static bool TryAppendHigherRarityRewardCard(
        Player player,
        List<CardCreationResult> rewardCards,
        CardCreationOptions options)
    {
        if (rewardCards.Count >= ExtraRewardCardOptionCount)
            return false;

        var lowestRarity = rewardCards
            .Select(result => result.Card.Rarity)
            .OrderBy(GetRewardRarityRank)
            .FirstOrDefault();
        var targetRarity = GetNextHigherRarity(lowestRarity);

        var excludedIds = rewardCards
            .Select(result => result.Card.CanonicalInstance?.Id ?? result.Card.Id)
            .ToHashSet();

        foreach (var rarity in EnumerateFallbackRarities(targetRarity))
        {
            var candidates = GetRewardCandidates(player, options, rarity, excludedIds);
            if (candidates.Count == 0)
                continue;

            var rng = options.RngOverride ?? player.PlayerRng.Rewards;
            var selectedCanonical = rng.NextItem(candidates);
            if (selectedCanonical == null)
                continue;

            var createdCard = player.RunState.CreateCard(selectedCanonical, player);
            rewardCards.Add(new CardCreationResult(createdCard));
            return true;
        }

        return false;
    }

    private static List<CardModel> GetRewardCandidates(
        Player player,
        CardCreationOptions options,
        CardRarity rarity,
        HashSet<ModelId> excludedIds)
    {
        IEnumerable<CardModel> source = rarity == CardRarity.Ancient
            ? ModelDb.AllCards.Where(IsBaseGameCard)
            : options.GetPossibleCards(player);

        return source
            .Where(card => card.Rarity == rarity)
            .Where(card => IsRewardEligibleRarity(card.Rarity))
            .Where(card => !excludedIds.Contains(card.Id))
            .GroupBy(card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(group => group.First())
            .ToList();
    }

    private static IEnumerable<CardRarity> EnumerateFallbackRarities(CardRarity startingRarity)
    {
        for (var current = startingRarity; current != CardRarity.None; current = GetNextLowerRarity(current))
        {
            yield return current;
            if (current == CardRarity.Common)
                yield break;
        }
    }

    private static CardRarity GetNextHigherRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => CardRarity.Uncommon,
            CardRarity.Uncommon => CardRarity.Rare,
            CardRarity.Rare => CardRarity.Ancient,
            CardRarity.Ancient => CardRarity.Ancient,
            _ => CardRarity.Common
        };
    }

    private static CardRarity GetNextLowerRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Ancient => CardRarity.Rare,
            CardRarity.Rare => CardRarity.Uncommon,
            CardRarity.Uncommon => CardRarity.Common,
            CardRarity.Common => CardRarity.None,
            _ => CardRarity.None
        };
    }

    private static bool IsRewardEligibleRarity(CardRarity rarity)
    {
        return rarity is CardRarity.Common or CardRarity.Uncommon or CardRarity.Rare or CardRarity.Ancient;
    }

    private static bool IsBaseGameCard(CardModel card)
    {
        return card.GetType().Assembly == typeof(CardModel).Assembly;
    }

    private static int GetRewardRarityRank(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => 1,
            CardRarity.Uncommon => 2,
            CardRarity.Rare => 3,
            CardRarity.Ancient => 4,
            _ => 0
        };
    }
}
