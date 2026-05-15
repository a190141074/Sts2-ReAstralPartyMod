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
    private const int DefaultRerolls = 1;

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
        var selectionTitle = new LocString("relics", $"{Id.Entry}.selectionScreenHeader").GetRawText();
        var selectionResult = await DeterministicMultiplayerChoiceHelper.SelectRefreshableRelicForPlayer(
            Owner,
            rewardOptions,
            DefaultRerolls,
            selectionTitle,
            "剩余刷新次数：",
            "筹码概率：蓝 60% / 紫 30% / 金 10%",
            $"{Id.Entry}.pack-choice",
            RerollRewardOptions,
            RebuildRewardOptionsFromHistory);
        if (selectionResult.SelectedRelic == null)
            return;

        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, selectionResult.SelectedRelic);
    }

    private List<RelicModel> BuildRewardOptions(Player owner)
    {
        return TokenRewardRerollHelper.BuildInitialOptions(
            owner,
            RewardChoiceCount,
            RollRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private IReadOnlyList<RelicModel> RerollRewardOptions(
        IReadOnlyList<RelicModel> currentOptions,
        int rerollOrdinal,
        IReadOnlySet<ModelId> historicalIds)
    {
        return TokenRewardRerollHelper.RerollOptions(
            Owner!,
            currentOptions,
            rerollOrdinal + 1,
            historicalIds,
            RollRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private IReadOnlyList<RelicModel> RebuildRewardOptionsFromHistory(IReadOnlyList<int> rerollHistory)
    {
        return TokenRewardRerollHelper.RebuildOptionsFromHistory(
            Owner!,
            RewardChoiceCount,
            rerollHistory,
            RollRewardRarity,
            TokenRewardSelectionHelper.GetDefaultRaritySelectionOrder,
            GetCandidates);
    }

    private RelicRarity RollRewardRarity(Player owner, int slotIndex, int rerollOrdinal)
    {
        var totalWeight = CommonWeight + UncommonWeight + RareWeight;
        if (totalWeight <= 0)
            return RelicRarity.Common;

        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            totalWeight,
            MainFile.ModId,
            Id.Entry,
            "token_pack_rarity",
            owner.RunState.Rng.StringSeed,
            owner.RunState.CurrentActIndex,
            owner.NetId,
            slotIndex,
            rerollOrdinal);
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
            .Where(relic =>
                !excludeOwned || owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
}
