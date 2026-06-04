using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Rewards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StokovCardRewardRefreshHelper
{
    private sealed class EligibleRewardMarker;
    private const int CostPreviewCardCount = 3;

    private static readonly ConditionalWeakTable<CardReward, EligibleRewardMarker> EligibleRewards = new();
    private static readonly PropertyInfo? OptionsProperty =
        AccessTools.Property(typeof(CardReward), "Options");
    private static readonly FieldInfo? CardsField =
        AccessTools.Field(typeof(CardReward), "_cards");
    private static readonly FieldInfo? HasBeenRerolledField =
        AccessTools.Field(typeof(CardReward), "_hasBeenRerolled");

    public static bool TryEnableStandardCombatRewardReroll(
        Player owner,
        List<Reward> rewards,
        AbstractRoom? room)
    {
        if (room is not CombatRoom)
            return false;

        var modified = false;
        var markedOne = false;
        foreach (var reward in rewards.OfType<CardReward>())
        {
            if (markedOne || !IsStandardCombatReward(reward))
                continue;

            reward.CanReroll = true;
            EligibleRewards.GetValue(reward, static _ => new EligibleRewardMarker());
            markedOne = true;
            modified = true;
        }

        return modified;
    }

    public static bool TryReplaceRerollAlternative(
        Player owner,
        CardReward reward,
        List<CardRewardAlternative> alternatives)
    {
        if (!EligibleRewards.TryGetValue(reward, out _))
            return false;

        var rerollIndex = alternatives.FindIndex(static alternative =>
            string.Equals(alternative.OptionId, "REROLL", StringComparison.Ordinal));
        if (rerollIndex < 0)
            return false;

        if (!CanPayRerollCost(owner, reward, out _))
        {
            alternatives.RemoveAt(rerollIndex);
            return true;
        }

        alternatives[rerollIndex] = new CardRewardAlternative(
            "REROLL",
            () => TryRerollAsync(owner, reward),
            PostAlternateCardRewardAction.DoNothing);
        return true;
    }

    public static async Task TryRerollAsync(Player owner, CardReward reward)
    {
        if (!CanPayRerollCost(owner, reward, out var hpCost))
            return;
        if (owner.Creature == null)
            return;

        await CreatureCmd.SetCurrentHp(owner.Creature, owner.Creature.CurrentHp - hpCost);
        await PerformRepeatableRerollAsync(reward);
    }

    private static async Task PerformRepeatableRerollAsync(CardReward reward)
    {
        if (CardsField == null || HasBeenRerolledField == null)
        {
            await reward.Reroll();
            reward.CanReroll = true;
            return;
        }

        var currentCards = reward.Cards.ToArray();
        RecordSkippedCards(reward, currentCards);

        HasBeenRerolledField.SetValue(reward, true);
        if (CardsField.GetValue(reward) is IList cardsList)
            cardsList.Clear();

        reward.CanReroll = true;
        await reward.Populate();
        reward.CanReroll = true;
    }

    private static void RecordSkippedCards(CardReward reward, IReadOnlyList<CardModel> cards)
    {
        if (cards.Count == 0)
            return;

        var localNetId = LocalContext.NetId;
        var historyEntry = localNetId.HasValue
            ? reward.Player.RunState.CurrentMapPointHistoryEntry?.GetEntry(localNetId.Value)
            : null;

        foreach (var card in cards)
        {
            historyEntry?.CardChoices.Add(new CardChoiceHistoryEntry(card, wasPicked: false));
            RunManager.Instance?.RewardSynchronizer?.SyncLocalSkippedCard(card);
        }
    }

    private static bool CanPayRerollCost(Player owner, CardReward reward, out decimal hpCost)
    {
        hpCost = GetRerollCost(reward);
        return hpCost > 0m
               && owner.Creature != null
               && owner.Creature.CurrentHp > hpCost;
    }

    private static decimal GetRerollCost(CardReward reward)
    {
        return GetRerollCost(reward.Cards);
    }

    private static decimal GetRerollCost(IEnumerable<CardModel> cards)
    {
        var consideredCards = cards.Take(CostPreviewCardCount).ToArray();
        if (consideredCards.Length == 0)
            return 0m;

        var highestRarityRank = consideredCards.Max(static card => GetRarityRank(card.Rarity));
        return highestRarityRank switch
        {
            <= 1 => 1m,
            2 => 3m,
            _ => 7m
        };
    }

    private static int GetRarityRank(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => 1,
            CardRarity.Uncommon => 2,
            CardRarity.Rare => 3,
            CardRarity.Ancient => 4,
            _ => 1
        };
    }

    private static bool IsStandardCombatReward(CardReward reward)
    {
        var options = OptionsProperty?.GetValue(reward) as CardCreationOptions;
        return options?.Source == CardCreationSource.Encounter;
    }
}
