using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class TokenRewardSelectionHelper
{
    public static List<RelicModel> BuildRewardOptions(
        Player owner,
        int optionCount,
        Rng rng,
        Func<Rng, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        if (optionCount <= 0)
            return [];

        var selectedIds = new HashSet<ModelId>();
        var selectedRelics = new List<RelicModel>(optionCount);

        for (var slotIndex = 0; slotIndex < optionCount; slotIndex++)
        {
            var reward = GetRewardCandidate(owner, rng, selectedIds, rollRarity, getRaritySelectionOrder,
                getCandidates);
            if (reward == null)
                break;

            selectedRelics.Add(reward);
            selectedIds.Add(GetCanonicalId(reward));
        }

        return selectedRelics;
    }

    public static RelicModel? GetRewardCandidate(
        Player owner,
        Rng rng,
        IReadOnlySet<ModelId> selectedIds,
        Func<Rng, RelicRarity> rollRarity,
        Func<RelicRarity, IEnumerable<RelicRarity>> getRaritySelectionOrder,
        Func<Player, RelicRarity, bool, IReadOnlySet<ModelId>, List<RelicModel>> getCandidates)
    {
        var rolledRarity = rollRarity(rng);

        foreach (var rarity in getRaritySelectionOrder(rolledRarity))
        {
            var unownedCandidates = getCandidates(owner, rarity, true, selectedIds);
            if (unownedCandidates.Count > 0)
                return unownedCandidates[rng.NextInt(unownedCandidates.Count)];
        }

        foreach (var rarity in getRaritySelectionOrder(rolledRarity))
        {
            var anyCandidates = getCandidates(owner, rarity, false, selectedIds);
            if (anyCandidates.Count > 0)
                return anyCandidates[rng.NextInt(anyCandidates.Count)];
        }

        return null;
    }

    public static IEnumerable<RelicRarity> GetDefaultRaritySelectionOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Common, RelicRarity.Uncommon, RelicRarity.Rare })
            if (fallback != rolledRarity)
                yield return fallback;
    }

    public static ModelId GetCanonicalId(RelicModel relic)
    {
        return relic.CanonicalInstance?.Id ?? relic.Id;
    }
}
