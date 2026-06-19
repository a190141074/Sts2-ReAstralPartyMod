using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class EnigmaticDiscoveryRestSiteOption : AstralPartyRestSiteOptionModel
{
    public static string OptionKey => GetOptionId<EnigmaticDiscoveryRestSiteOption>();

    public EnigmaticDiscoveryRestSiteOption(Player owner) : base(owner)
    {
    }

    public override bool IsEnabled => true;

    public override LocString Description => RestSiteUiLoc("description");

    public override async Task<bool> OnSelect()
    {
        var relic = Owner.GetRelic<EnigmaticSevenBlessings>();
        if (relic == null)
            return false;

        IReadOnlyList<EnigmaticRestSiteMaterialReward> rewards;
        try
        {
            rewards = relic.CreateDiscoveryRewardResults(Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[EnigmaticDiscoveryRestSiteOption] Failed to build rewards | owner={Owner.NetId} | error={ex.Message}");
            return false;
        }

        if (rewards.Count == 0)
            return false;

        var rewardScreenRewards = rewards
            .Select(reward => EnigmaticRewardRegistry.CreateUniqueMaterialReward(Owner, reward.Kind, reward.Amount))
            .ToList();

        MainFile.Logger.Info(
            $"[EnigmaticDiscoveryRestSiteOption] discovery rewards resolved for rest-site reward screen | owner={Owner.NetId} | rewardCount={rewardScreenRewards.Count}");
        await RewardsCmd.OfferCustom(Owner, rewardScreenRewards);
        relic.Flash();
        return true;
    }
}
