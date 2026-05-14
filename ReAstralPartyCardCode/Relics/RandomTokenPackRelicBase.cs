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
        return TokenRewardSelectionHelper.BuildRewardOptions(
            owner,
            RewardChoiceCount,
            owner.PlayerRng.Rewards,
            RollRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
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

    private static List<RelicModel> GetCandidates(
        Player owner,
        RelicRarity rarity,
        bool excludeOwned,
        IReadOnlySet<ModelId> selectedIds)
    {
        return TokenRelicRegistry.GetAvailableNonDiceTokenRelicsByRarity(owner.RunState, rarity)
            .Where(relic => !TokenRelicRegistry.IsSeriesTokenRelic(relic))
            .Where(relic => !selectedIds.Contains(TokenRewardSelectionHelper.GetCanonicalId(relic)))
            .Where(relic => !excludeOwned || owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
}
