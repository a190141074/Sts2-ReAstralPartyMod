using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class EnigmaticDiscoveryRestSiteOption : AstralPartyRestSiteOptionModel
{
    public static string OptionKey => GetOptionId<EnigmaticDiscoveryRestSiteOption>();

    public EnigmaticDiscoveryRestSiteOption(Player owner) : base(owner)
    {
        IsEnabled = true;
    }

    public override LocString Description => RestSiteUiLoc("description");

    public override async Task<bool> OnSelect()
    {
        var relic = Owner.GetRelic<EnigmaticSevenBlessings>();
        if (relic == null)
            return false;

        IReadOnlyList<Reward> rewards;
        try
        {
            rewards = relic.CreateDiscoveryRewards(Owner);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[EnigmaticDiscoveryRestSiteOption] Failed to build rewards | owner={Owner.NetId} | error={ex.Message}");
            return false;
        }

        if (rewards.Count == 0)
            return false;

        await RewardsCmd.OfferCustom(Owner, rewards.ToList());
        relic.Flash();
        MainFile.Logger.Info($"[EnigmaticDiscoveryRestSiteOption] Offered discovery rewards | owner={Owner.NetId} | rewardCount={rewards.Count}");
        return true;
    }
}
