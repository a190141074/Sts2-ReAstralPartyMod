using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rewards;
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

        var selectedCanonicalCard = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            new ThrowingPlayerChoiceContext(),
            Owner,
            deckCards,
            true,
            $"{OptionKey}.deck");
        if (selectedCanonicalCard == null)
        {
            RefreshEnabled();
            return false;
        }

        var selectedDeckCard = EnigmaticOblivionDeckHelper.FindMatchingDeckCard(Owner, selectedCanonicalCard);
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
            await RewardsCmd.OfferCustom(Owner, rewards);

        RefreshEnabled();
        return true;
    }

    private List<Reward> CreateRewards()
    {
        var rewards = new List<Reward>();
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
            rewards.Add(EnigmaticRewardRegistry.CreateUniqueMaterialReward(Owner, kind, amount));
        }

        return rewards;
    }

    private void RefreshEnabled()
    {
        IsEnabled = Owner.GetRelic<EnigmaticVoidStone>()?.CanUseOblivion(Owner) ?? false;
    }
}
