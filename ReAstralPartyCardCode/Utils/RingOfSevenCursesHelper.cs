using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Potions;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class RingOfSevenCursesHelper
{
    public const string SeriesId = "ring_of_seven_curses";
    private const int BonusPersonaChestPermille = 70;
    private static readonly PotionRarity[] SevenBlessingsPotionRarityPattern =
    [
        PotionRarity.Common,
        PotionRarity.Uncommon,
        PotionRarity.Uncommon,
        PotionRarity.Rare
    ];

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

    public static async Task EnsureSeriesIntegrityAsync(Player? owner)
    {
        if (owner == null)
            return;

        var curses = owner.GetRelic<EnigmaticSevenCurses>();
        var blessings = owner.GetRelic<EnigmaticSevenBlessings>();
        if (curses == null && blessings == null)
            return;

        if (curses == null)
            await ObtainRelicIgnoringBanAsync<EnigmaticSevenCurses>(owner);
        else if (blessings == null)
            await ObtainRelicIgnoringBanAsync<EnigmaticSevenBlessings>(owner);

        SyncSeriesRewardFlags(owner);
    }

    public static bool ShouldGrantSevenCursesMaxHpBonus(Player? owner, EnigmaticSevenCurses self)
    {
        return !(self.AstralParty_SevenCursesMaxHpBonusGranted ||
                 owner?.GetRelic<EnigmaticSevenBlessings>()?.AstralParty_SevenCursesMaxHpBonusGranted == true);
    }

    public static bool ShouldGrantSevenBlessingsPotionSlots(Player? owner, EnigmaticSevenBlessings self)
    {
        return !(self.AstralParty_SevenBlessingsPotionSlotsGranted ||
                 owner?.GetRelic<EnigmaticSevenCurses>()?.AstralParty_SevenBlessingsPotionSlotsGranted == true);
    }

    public static void MarkSevenCursesMaxHpBonusGranted(Player? owner)
    {
        if (owner == null)
            return;

        var curses = owner.GetRelic<EnigmaticSevenCurses>();
        if (curses != null)
            curses.AstralParty_SevenCursesMaxHpBonusGranted = true;

        var blessings = owner.GetRelic<EnigmaticSevenBlessings>();
        if (blessings != null)
            blessings.AstralParty_SevenCursesMaxHpBonusGranted = true;
    }

    public static void MarkSevenBlessingsPotionSlotsGranted(Player? owner)
    {
        if (owner == null)
            return;

        var curses = owner.GetRelic<EnigmaticSevenCurses>();
        if (curses != null)
            curses.AstralParty_SevenBlessingsPotionSlotsGranted = true;

        var blessings = owner.GetRelic<EnigmaticSevenBlessings>();
        if (blessings != null)
            blessings.AstralParty_SevenBlessingsPotionSlotsGranted = true;
    }

    public static void GrantSevenBlessingsRandomPotions(Player? owner, int amount)
    {
        if (owner == null || amount <= 0)
            return;

        var slotsToGrant = Math.Min(amount, SevenBlessingsPotionRarityPattern.Length);
        for (var i = 0; i < slotsToGrant; i++)
        {
            var rarity = SevenBlessingsPotionRarityPattern[i];
            var candidates = GetSevenBlessingsSharedPotionCandidates(owner, rarity);
            if (candidates.Count == 0)
            {
                MainFile.Logger.Warn(
                    $"[RingOfSevenCursesHelper] No shared potion candidates for Seven Blessings | owner={owner.NetId} | rarity={rarity}");
                continue;
            }

            var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                candidates.Count,
                MainFile.ModId,
                SeriesId,
                nameof(GrantSevenBlessingsRandomPotions),
                rarity.ToString(),
                owner.RunState?.Rng.StringSeed,
                owner.RunState?.CurrentActIndex,
                owner.RunState?.TotalFloor,
                owner.NetId,
                i);
            var selected = candidates[roll];
            owner.AddPotionInternal(selected.ToMutable());
        }

        if (!RollPermille(
                BonusPersonaChestPermille,
                MainFile.ModId,
                SeriesId,
                nameof(GrantSevenBlessingsRandomPotions),
                "bonus_persona_chest",
                owner.RunState?.Rng.StringSeed,
                owner.RunState?.CurrentActIndex,
                owner.RunState?.TotalFloor,
                owner.NetId))
            return;

        PlayerCmd.GainMaxPotionCount(1, owner);
        owner.AddPotionInternal(ModelDb.Potion<PersonChestChoose>().ToMutable());
    }

    public static void SyncSeriesRewardFlags(Player? owner)
    {
        if (owner == null)
            return;

        var curses = owner.GetRelic<EnigmaticSevenCurses>();
        var blessings = owner.GetRelic<EnigmaticSevenBlessings>();
        if (curses == null && blessings == null)
            return;

        var sevenCursesGranted =
            (curses?.AstralParty_SevenCursesMaxHpBonusGranted ?? false) ||
            (blessings?.AstralParty_SevenCursesMaxHpBonusGranted ?? false);
        var sevenBlessingsGranted =
            (curses?.AstralParty_SevenBlessingsPotionSlotsGranted ?? false) ||
            (blessings?.AstralParty_SevenBlessingsPotionSlotsGranted ?? false);

        if (curses != null)
        {
            curses.AstralParty_SevenCursesMaxHpBonusGranted = sevenCursesGranted;
            curses.AstralParty_SevenBlessingsPotionSlotsGranted = sevenBlessingsGranted;
        }

        if (blessings != null)
        {
            blessings.AstralParty_SevenCursesMaxHpBonusGranted = sevenCursesGranted;
            blessings.AstralParty_SevenBlessingsPotionSlotsGranted = sevenBlessingsGranted;
        }
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
        var highestRarity = rewardCards
            .Select(result => result.Card.Rarity)
            .OrderByDescending(GetRewardRarityRank)
            .FirstOrDefault();
        var targetRarity = GetNextHigherRarity(highestRarity);

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

    public static bool ShouldAppendHigherRarityRewardCard(Player? player, CardCreationOptions options)
    {
        // Some relic/ancient card-pick flows enforce a strict candidate cap, so Seven Blessings only augments
        // normal encounter card rewards.
        return player != null && options.Source == CardCreationSource.Encounter;
    }

    private static async Task ObtainRelicIgnoringBanAsync<TMissing>(Player owner)
        where TMissing : RelicModel
    {
        if (owner.GetRelic<TMissing>() != null)
            return;

        var canonicalRelic = ModelDb.Relic<TMissing>().CanonicalInstance ?? ModelDb.Relic<TMissing>();
        ExclusiveRelicUnlockHelper.MarkRelicUnlockedForCurrentRunAndProfile(owner, canonicalRelic);
        SaveManager.Instance?.MarkRelicAsSeen(canonicalRelic);
        await RelicCmd.Obtain(canonicalRelic.ToMutable(), owner);
    }

    private static List<PotionModel> GetSevenBlessingsSharedPotionCandidates(Player owner, PotionRarity rarity)
    {
        var unlockState = owner.RunState?.UnlockState;
        if (unlockState == null)
            return [];

        return ModelDb.AllPotionPools
            .SelectMany(pool => pool.GetUnlockedPotions(unlockState))
            .Where(IsBaseGamePotion)
            .Where(potion => potion.Rarity == rarity)
            .GroupBy(potion => potion.CanonicalInstance?.Id ?? potion.Id)
            .Select(group => group.First())
            .OrderBy(potion => (potion.CanonicalInstance?.Id ?? potion.Id).Entry, StringComparer.Ordinal)
            .ToList();
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

    private static bool IsBaseGamePotion(PotionModel potion)
    {
        return potion.GetType().Assembly == typeof(PotionModel).Assembly;
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
