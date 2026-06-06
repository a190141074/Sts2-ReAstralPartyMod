using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class EnigmaticOblivionRestSiteOption : AstralPartyRestSiteOptionModel
{
    public static string OptionKey => GetOptionId<EnigmaticOblivionRestSiteOption>();

    public EnigmaticOblivionRestSiteOption(Player owner) : base(owner)
    {
        RefreshEnabled();
    }

    public override LocString Description => RestSiteUiLoc("description");

    public override async Task<bool> OnSelect()
    {
        RefreshEnabled();
        if (!IsEnabled)
            return false;

        var relic = Owner.GetRelic<EnigmaticVoidStone>();
        if (relic == null)
            return false;

        var deckCards = EventDeckCardHelper.GetRunDeckCards(Owner);
        if (deckCards.Count == 0)
        {
            RefreshEnabled();
            return false;
        }

        var selectedCards = await CardSelectCmd.FromDeckGeneric(
            Owner,
            new CardSelectorPrefs(Description, 1)
            {
                Cancelable = true,
                RequireManualConfirmation = true
            },
            static _ => true);
        var selectedDeckCard = selectedCards.FirstOrDefault();
        if (selectedDeckCard == null)
        {
            RefreshEnabled();
            return false;
        }

        await EventDeckCardMutationHelper.Remove(Owner, [selectedDeckCard], $"{OptionKey}.remove");
        relic.RecordObliviatedCard(selectedDeckCard);
        relic.Flash();

        var rewards = CreateRewards();
        if (rewards.Count > 0)
        {
            var rewardScreenRewards = rewards
                .Select(reward => EnigmaticRewardRegistry.CreateUniqueMaterialReward(Owner, reward.Kind, reward.Amount))
                .ToList();

            MainFile.Logger.Info(
                $"[EnigmaticOblivionRestSiteOption] oblivion rewards resolved for rest-site reward screen | owner={Owner.NetId} | rewardCount={rewardScreenRewards.Count}");
            await RewardsCmd.OfferCustom(Owner, rewardScreenRewards);
        }

        RefreshEnabled();
        return true;
    }

    private List<EnigmaticRestSiteMaterialReward> CreateRewards()
    {
        var rewards = new List<EnigmaticRestSiteMaterialReward>();
        var rolledKinds = new HashSet<EnigmaticUniqueMaterialKind>();
        for (var slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            var kind = EnigmaticRewardRegistry.RollUniqueMaterialKindWithBonuses(
                Owner,
                0,
                0,
                0,
                rolledKinds,
                MainFile.ModId,
                OptionKey,
                "reward_kind",
                Owner.RunState?.Rng.StringSeed,
                Owner.RunState?.CurrentActIndex ?? 0,
                Owner.RunState?.TotalFloor ?? 0,
                Owner.NetId,
                slotIndex);
            rolledKinds.Add(kind);

            var amount = EnigmaticRewardRegistry.RollRewardAmount(
                kind,
                MainFile.ModId,
                OptionKey,
                "reward_amount",
                Owner.RunState?.Rng.StringSeed,
                Owner.RunState?.CurrentActIndex ?? 0,
                Owner.RunState?.TotalFloor ?? 0,
                Owner.NetId,
                slotIndex);
            rewards.Add(new EnigmaticRestSiteMaterialReward(kind, amount));
        }

        return rewards;
    }

    private void RefreshEnabled()
    {
        IsEnabled = Owner.GetRelic<EnigmaticVoidStone>()?.CanUseOblivion(Owner) ?? false;
    }
}
