using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Patches;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class RandomTokenPackRelicBase : AstralPartyRelicModel
{
    private const int RewardChoiceCount = 3;

    protected abstract int CommonWeight { get; }
    protected abstract int UncommonWeight { get; }
    protected abstract int RareWeight { get; }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
            return;

        var rewardOptions = BuildRewardOptions(Owner);
        if (rewardOptions.Count == 0)
            return;

        Flash();
        using var _ = RelicSelectionHeaderContext.Push(
            new LocString("relics", $"{Id.Entry}.selectionScreenHeader").GetRawText());

        var reward = await DeterministicMultiplayerChoiceHelper.SelectRelicForPlayer(
            Owner,
            rewardOptions,
            $"{Id.Entry}.pack-choice");
        if (reward == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, reward);
    }

    private List<RelicModel> BuildRewardOptions(Player owner)
    {
        var selectedIds = new HashSet<ModelId>();
        var selectedRelics = new List<RelicModel>(RewardChoiceCount);
        var rng = owner.PlayerRng.Rewards;

        for (var slotIndex = 0; slotIndex < RewardChoiceCount; slotIndex++)
        {
            var reward = GetRewardCandidate(owner, rng, selectedIds);
            if (reward == null)
                break;

            selectedRelics.Add(reward);
            selectedIds.Add(GetCanonicalId(reward));
        }

        return selectedRelics;
    }

    private RelicModel? GetRewardCandidate(Player owner, Rng rng, IReadOnlySet<ModelId> selectedIds)
    {
        var rolledRarity = RollRewardRarity(rng);

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var unownedCandidates = GetCandidates(owner, rarity, excludeOwned: true, selectedIds);
            if (unownedCandidates.Count > 0)
                return unownedCandidates[rng.NextInt(unownedCandidates.Count)];
        }

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var anyCandidates = GetCandidates(owner, rarity, excludeOwned: false, selectedIds);
            if (anyCandidates.Count > 0)
                return anyCandidates[rng.NextInt(anyCandidates.Count)];
        }

        return null;
    }

    private RelicRarity RollRewardRarity(Rng rng)
    {
        var totalWeight = CommonWeight + UncommonWeight + RareWeight;
        if (totalWeight <= 0)
            return RelicRarity.Common;

        var roll = rng.NextInt(totalWeight);
        if (roll < CommonWeight)
            return RelicRarity.Common;
        if (roll < CommonWeight + UncommonWeight)
            return RelicRarity.Uncommon;

        return RelicRarity.Rare;
    }

    private static IEnumerable<RelicRarity> GetRaritySelectionOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Common, RelicRarity.Uncommon, RelicRarity.Rare })
            if (fallback != rolledRarity)
                yield return fallback;
    }

    private static List<RelicModel> GetCandidates(
        Player owner,
        RelicRarity rarity,
        bool excludeOwned,
        IReadOnlySet<ModelId> selectedIds)
    {
        return TokenRelicRegistry.GetAvailableNonDiceTokenRelicsByRarity(owner.RunState, rarity)
            .Where(relic => !TokenRelicRegistry.IsSeriesTokenRelic(relic))
            .Where(relic => !selectedIds.Contains(GetCanonicalId(relic)))
            .Where(relic => !excludeOwned || owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    private static ModelId GetCanonicalId(RelicModel relic)
    {
        return relic.CanonicalInstance?.Id ?? relic.Id;
    }
}
