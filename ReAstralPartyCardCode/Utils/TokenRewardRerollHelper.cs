using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenRewardRerollHelper
{
    public static List<RelicModel> BuildInitialOptions(
        Player owner,
        int optionCount,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        return BuildOptions(owner, optionCount, 0, new HashSet<ModelId>(), rollRarity, getRaritySelectionOrder, getCandidates);
    }

    public static List<RelicModel> RerollOptions(
        Player owner,
        IReadOnlyList<RelicModel> currentOptions,
        int rerollOrdinal,
        IReadOnlySet<ModelId> historicalIds,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        return BuildOptions(
            owner,
            currentOptions.Count,
            rerollOrdinal,
            historicalIds,
            rollRarity,
            getRaritySelectionOrder,
            getCandidates);
    }

    public static List<RelicModel> RebuildOptionsFromHistory(
        Player owner,
        int optionCount,
        IReadOnlyList<int> rerollHistory,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        var options = BuildOptions(owner, optionCount, 0, new HashSet<ModelId>(), rollRarity, getRaritySelectionOrder, getCandidates);
        var seenIds = options
            .Select(TokenRewardSelectionHelper.GetCanonicalId)
            .ToHashSet();

        for (var rerollOrdinal = 0; rerollOrdinal < rerollHistory.Count; rerollOrdinal++)
        {
            var slotIndex = rerollHistory[rerollOrdinal];
            options = RerollSingleOption(
                owner,
                options,
                slotIndex,
                rerollOrdinal,
                seenIds,
                rollRarity,
                getRaritySelectionOrder,
                getCandidates);
            if (options.Count == 0)
                break;

            seenIds.UnionWith(options.Select(TokenRewardSelectionHelper.GetCanonicalId));
        }

        return options;
    }

    private static List<RelicModel> RerollSingleOption(
        Player owner,
        IReadOnlyList<RelicModel> currentOptions,
        int slotIndex,
        int rerollOrdinal,
        IReadOnlySet<ModelId> seenIds,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        if (slotIndex < 0 || slotIndex >= currentOptions.Count)
            return currentOptions.ToList();

        var updated = BuildOptions(
            owner,
            currentOptions.Count,
            rerollOrdinal + 1,
            seenIds,
            rollRarity,
            getRaritySelectionOrder,
            getCandidates);
        return updated.Count == 0 ? currentOptions.ToList() : updated;
    }

    public static List<RelicModel> RebuildOptionsFromHistory(
        Player owner,
        int optionCount,
        IReadOnlyList<int> rerollHistory,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates,
        IReadOnlySet<ModelId> initialSeenIds)
    {
        var options = BuildOptions(owner, optionCount, 0, initialSeenIds, rollRarity, getRaritySelectionOrder, getCandidates);
        var seenIds = options
            .Select(TokenRewardSelectionHelper.GetCanonicalId)
            .ToHashSet();
        seenIds.UnionWith(initialSeenIds);

        for (var rerollOrdinal = 0; rerollOrdinal < rerollHistory.Count; rerollOrdinal++)
        {
            var slotIndex = rerollHistory[rerollOrdinal];
            options = RerollSingleOption(
                owner,
                options,
                slotIndex,
                rerollOrdinal,
                seenIds,
                rollRarity,
                getRaritySelectionOrder,
                getCandidates);
            if (options.Count == 0)
                break;

            seenIds.UnionWith(options.Select(TokenRewardSelectionHelper.GetCanonicalId));
        }

        return options;
    }

    private static List<RelicModel> BuildOptions(
        Player owner,
        int optionCount,
        int rerollOrdinal,
        IReadOnlySet<ModelId> historicalIds,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        if (optionCount <= 0)
            return [];

        var seenIds = historicalIds.Count == 0 ? [] : new HashSet<ModelId>(historicalIds);
        var selected = new List<RelicModel>(optionCount);
        for (var slotIndex = 0; slotIndex < optionCount; slotIndex++)
        {
            var reward = GetRewardCandidate(
                owner,
                slotIndex,
                rerollOrdinal,
                seenIds,
                rollRarity,
                getRaritySelectionOrder,
                getCandidates);
            if (reward == null)
                break;

            selected.Add(reward);
            seenIds.Add(TokenRewardSelectionHelper.GetCanonicalId(reward));
        }

        return selected;
    }

    private static RelicModel? GetRewardCandidate(
        Player owner,
        int slotIndex,
        int rerollOrdinal,
        IReadOnlySet<ModelId> selectedIds,
        Func<Player, int, int, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        var rolledRarity = rollRarity(owner, slotIndex, rerollOrdinal);

        foreach (var rarity in getRaritySelectionOrder(rolledRarity))
        {
            var unownedCandidates = getCandidates(owner, rarity, true, selectedIds);
            if (unownedCandidates.Count > 0)
                return PickDeterministically(owner, unownedCandidates, slotIndex, rerollOrdinal, rolledRarity);
        }

        foreach (var rarity in getRaritySelectionOrder(rolledRarity))
        {
            var anyCandidates = getCandidates(owner, rarity, false, selectedIds);
            if (anyCandidates.Count > 0)
                return PickDeterministically(owner, anyCandidates, slotIndex, rerollOrdinal, rolledRarity);
        }

        return null;
    }

    private static RelicModel PickDeterministically(
        Player owner,
        IReadOnlyList<RelicModel> candidates,
        int slotIndex,
        int rerollOrdinal,
        RelicRarity rolledRarity)
    {
        var index = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            "token_reward_reroll",
            owner.RunState.Rng.StringSeed,
            owner.RunState.CurrentActIndex,
            owner.NetId,
            slotIndex,
            rerollOrdinal,
            (int)rolledRarity,
            string.Join(",", candidates.Select(relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry)));
        return candidates[index];
    }
}
