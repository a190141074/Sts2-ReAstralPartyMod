using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract class RandomTokenPackRelicBase : AstralPartyRelicModel
{
    protected abstract int CommonWeight { get; }
    protected abstract int UncommonWeight { get; }
    protected abstract int RareWeight { get; }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (Owner == null)
            return;

        var reward = GetRandomTokenReward(Owner);
        if (reward == null)
            return;

        Flash();
        await PersonaMultiplayerEffectHelper.ObtainRelicDeterministic(Owner, reward);
    }

    private RelicModel? GetRandomTokenReward(Player owner)
    {
        var rng = owner.PlayerRng.Rewards;
        var rolledRarity = RollRewardRarity(rng);

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var unownedCandidates = GetCandidates(owner, rarity, excludeOwned: true);
            if (unownedCandidates.Count > 0)
                return unownedCandidates[rng.NextInt(unownedCandidates.Count)];
        }

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var anyCandidates = GetCandidates(owner, rarity, excludeOwned: false);
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

    private static List<RelicModel> GetCandidates(Player owner, RelicRarity rarity, bool excludeOwned)
    {
        return TokenRelicRegistry.GetAvailableNonDiceTokenRelicsByRarity(owner.RunState, rarity)
            .Where(relic => !TokenRelicRegistry.IsSeriesTokenRelic(relic))
            .Where(relic => !excludeOwned || owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
}
