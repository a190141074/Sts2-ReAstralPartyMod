using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.RestSite;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenGoldInitialPoint : AstralPartyRelicModel
{
    private const int MaxAscensions = 3;
    private const int FirstAscensionCost = 60;
    private const int SecondAscensionCost = 90;
    private const int ThirdAscensionCost = 120;
    private const decimal HealPercent = 0.15m;

    [SavedProperty] public int AstralParty_TokenGoldInitialPointAscensionCount { get; set; }

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBasePath => "res://AstralPartyMod/images/relic/personality_land_center";

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        AstralParty_TokenGoldInitialPointAscensionCount = 0;
    }

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (Owner == null || player != Owner)
            return false;

        options.Add(new InitialPointRestSiteOption(player, this));
        return true;
    }

    public bool IsRestSiteOptionEnabled()
    {
        return Owner != null && (IsMaxedAscension() || Owner.Gold >= GetCurrentAscensionCost());
    }

    public LocString BuildRestSiteDescription()
    {
        return new LocString("rest_site_ui", GetDescriptionKey());
    }

    public async Task<bool> ResolveRestSiteOptionSelection()
    {
        if (Owner?.Creature == null)
            return false;

        Flash();

        if (!IsMaxedAscension())
        {
            var goldCost = GetCurrentAscensionCost();
            if (Owner.Gold < goldCost)
                return false;

            var reward = GetRandomAscensionReward();
            if (reward == null)
                return false;

            await PlayerCmd.LoseGold(goldCost, Owner, GoldLossType.Spent);
            await RewardSyncHelper.ObtainRelicAsReward(Owner, reward);
            AstralParty_TokenGoldInitialPointAscensionCount =
                Math.Min(MaxAscensions, AstralParty_TokenGoldInitialPointAscensionCount + 1);
        }

        await HealOwner();
        return true;
    }

    private bool IsMaxedAscension()
    {
        return AstralParty_TokenGoldInitialPointAscensionCount >= MaxAscensions;
    }

    private int GetCurrentAscensionCost()
    {
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => FirstAscensionCost,
            1 => SecondAscensionCost,
            2 => ThirdAscensionCost,
            _ => 0
        };
    }

    private string GetDescriptionKey()
    {
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => $"{InitialPointRestSiteOption.OptionKey}.description_stage1",
            1 => $"{InitialPointRestSiteOption.OptionKey}.description_stage2",
            2 => $"{InitialPointRestSiteOption.OptionKey}.description_stage3",
            _ => $"{InitialPointRestSiteOption.OptionKey}.description_maxed"
        };
    }

    private async Task HealOwner()
    {
        var healAmount = Math.Max(1m, Math.Ceiling(Owner!.Creature.MaxHp * HealPercent));
        await CreatureCmd.Heal(Owner.Creature, healAmount, true);
    }

    private RelicModel? GetRandomAscensionReward()
    {
        if (Owner == null)
            return null;

        var rng = Owner.PlayerRng.Rewards;
        var rolledRarity = RollAscensionRewardRarity(rng);

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var unownedCandidates = GetCandidates(rarity, excludeOwned: true);
            if (unownedCandidates.Count > 0)
                return unownedCandidates[rng.NextInt(unownedCandidates.Count)];
        }

        foreach (var rarity in GetRaritySelectionOrder(rolledRarity))
        {
            var anyCandidates = GetCandidates(rarity, excludeOwned: false);
            if (anyCandidates.Count > 0)
                return anyCandidates[rng.NextInt(anyCandidates.Count)];
        }

        return null;
    }

    private RelicRarity RollAscensionRewardRarity(Rng rng)
    {
        var roll = rng.NextInt(100);
        return AstralParty_TokenGoldInitialPointAscensionCount switch
        {
            0 => roll < 60 ? RelicRarity.Common : roll < 90 ? RelicRarity.Uncommon : RelicRarity.Rare,
            1 => roll < 40 ? RelicRarity.Common : roll < 80 ? RelicRarity.Uncommon : RelicRarity.Rare,
            2 => roll < 20 ? RelicRarity.Common : roll < 70 ? RelicRarity.Uncommon : RelicRarity.Rare,
            _ => RelicRarity.Rare
        };
    }

    private static IEnumerable<RelicRarity> GetRaritySelectionOrder(RelicRarity rolledRarity)
    {
        yield return rolledRarity;

        foreach (var fallback in new[] { RelicRarity.Common, RelicRarity.Uncommon, RelicRarity.Rare })
        {
            if (fallback != rolledRarity)
                yield return fallback;
        }
    }

    private List<RelicModel> GetCandidates(RelicRarity rarity, bool excludeOwned)
    {
        return TokenRelicRegistry.GetTokenRelicsByRarity(rarity)
            .Where(relic => IsValidRewardCandidate(relic, excludeOwned))
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    private bool IsValidRewardCandidate(RelicModel relic, bool excludeOwned)
    {
        if (Owner == null)
            return false;
        if (relic.Rarity is not (RelicRarity.Common or RelicRarity.Uncommon or RelicRarity.Rare))
            return false;
        if (relic.CanonicalInstance.Id == CanonicalInstance.Id)
            return false;

        return !excludeOwned || Owner.Relics.All(owned => owned.CanonicalInstance.Id != relic.CanonicalInstance.Id);
    }
}
