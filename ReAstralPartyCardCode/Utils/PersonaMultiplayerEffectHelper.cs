using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Helpers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaMultiplayerEffectHelper
{
    private const int DuplicateInitialPointFallbackEternalStarlightStacks = 3;

    public static Task AddGeneratedCardToHandAndNotify(
        CardModel card,
        bool animate = true,
        CardPilePosition position = CardPilePosition.Top,
        AbstractModel? source = null)
    {
        return GeneratedCardObserver.AddGeneratedCardToHandAndNotify(card, animate, position, source);
    }

    public static async Task<bool> TryRedirectLivingFolioCopyToDerivativeStacks(
        Player? owner,
        CardModel? sourceCard,
        AbstractModel? source)
    {
        if (owner == null || sourceCard == null)
            return false;

        var canonicalCard = sourceCard.CanonicalInstance ?? sourceCard;
        if (canonicalCard.Id != ModelDb.GetId<SkillLivingFolio>())
            return false;

        var livingFolioRelic = owner.GetRelic<PersonalityDerivativeLivingFolio>()
                               ?? await ObtainDerivativeRelicIfMissing<PersonalityDerivativeLivingFolio>(owner)
                               as PersonalityDerivativeLivingFolio;
        if (livingFolioRelic == null)
            return false;

        await CardGainAttribution.RunWithSource(source, () =>
        {
            livingFolioRelic.AddStacksCapped(1);
            return Task.CompletedTask;
        });
        return true;
    }

    public static Task<IEnumerable<CardModel>> DrawCardsForPlayer(
        PlayerChoiceContext choiceContext,
        decimal count,
        Player player,
        AbstractModel? source)
    {
        return CardGainAttribution.RunWithSource(source, () => CardPileCmd.Draw(choiceContext, count, player));
    }

    public static Task MoveCombatCardToHandAndNotify(
        Player recipient,
        CardModel card,
        CardPilePosition position,
        AbstractModel? source)
    {
        return CardGainAttribution.RunWithSource(source, async () =>
        {
            await CardPileCmd.Add(card, PileType.Hand.GetPile(recipient), position, source);
            await GeneratedCardObserver.NotifyCardAddedToHand(card, source);
        });
    }

    public static Task GainGoldDeterministic(decimal amount, Player owner)
    {
        return PlayerCmd.GainGold(amount, owner);
    }

    public static Task LoseGoldDeterministic(decimal amount, Player owner, GoldLossType lossType)
    {
        return PlayerCmd.LoseGold(amount, owner, lossType);
    }

    public static async Task<RelicModel?> ObtainDerivativeRelicIfMissing<T>(Player? owner)
        where T : RelicModel
    {
        if (owner == null)
            return null;

        var existing = owner.GetRelic<T>();
        if (existing != null)
            return existing;

        return await RelicCmd.Obtain(ModelDb.Relic<T>().ToMutable(), owner);
    }

    public static Task<RelicModel> ObtainRelicDeterministic(Player owner, RelicModel relic)
    {
        var canonicalRelic = relic.CanonicalInstance ?? relic;
        if (canonicalRelic.Id == ModelDb.GetId<TokenGoldInitialPoint>() && owner.GetRelic<TokenGoldInitialPoint>() != null)
            return ObtainDuplicateInitialPointFallback(owner);

        return RelicCmd.Obtain(canonicalRelic.ToMutable(), owner);
    }

    public static Task<RelicModel> ObtainRelicAsReward(Player owner, RelicModel relic)
    {
        GuardRewardSyncAllowed("relic reward");

        if (LocalContext.IsMe(owner))
            RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedRelic(relic);

        return RelicCmd.Obtain(relic.ToMutable(), owner);
    }

    public static void SyncGoldLostAsReward(Player owner, int goldLost)
    {
        GuardRewardSyncAllowed("gold loss");

        if (LocalContext.IsMe(owner))
            RunManager.Instance?.RewardSynchronizer?.SyncLocalGoldLost(goldLost);
    }

    public static IReadOnlyList<Player> GetStableCombatPlayers(Player owner)
    {
        return owner.Creature?.CombatState?.Players
                   .OrderBy(player => player.NetId)
                   .ToList()
               ?? [];
    }

    public static IReadOnlyList<RelicModel> CreateStableRelicChoiceOptions(
        IEnumerable<RelicModel> candidates,
        Rng rng,
        int maxCount)
    {
        var ordered = candidates
            .OrderBy(relic => relic.Id.Entry, StringComparer.Ordinal)
            .ToList();
        ordered.UnstableShuffle(rng);
        return ordered.Take(Math.Min(maxCount, ordered.Count)).ToList();
    }

    public static IReadOnlyList<RelicModel> CreateDeterministicRelicChoiceOptions(
        IEnumerable<RelicModel> candidates,
        int maxCount,
        params object?[] contextParts)
    {
        var ordered = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
                candidates,
                relic => (relic.CanonicalInstance?.Id ?? relic.Id).Entry,
                contextParts)
            .ToList();
        return ordered.Take(Math.Min(maxCount, ordered.Count)).ToList();
    }

    public static IReadOnlyList<RelicModel> CreateWeightedDeterministicRelicChoiceOptions(
        IEnumerable<RelicModel> candidates,
        int maxCount,
        Func<RelicModel, int> weightSelector,
        params object?[] contextParts)
    {
        var ordered = candidates
            .Select(relic => new
            {
                Relic = relic,
                Score = GetBestWeightedDeterministicScore(relic, Math.Max(1, weightSelector(relic)), contextParts),
                Id = (relic.CanonicalInstance?.Id ?? relic.Id).Entry
            })
            .OrderBy(entry => entry.Score)
            .ThenBy(entry => entry.Id, StringComparer.Ordinal)
            .Select(entry => entry.Relic)
            .ToList();

        return ordered.Take(Math.Min(maxCount, ordered.Count)).ToList();
    }

    public static CardModel? SelectRandomUpgradeableCombatCard(
        Player owner,
        Func<CardModel, bool> predicate,
        Rng rng)
    {
        var playerCombatState = owner.PlayerCombatState;
        if (playerCombatState == null)
            return null;

        var candidates = playerCombatState
            .AllCards
            .Where(card => card.Owner == owner && card.IsUpgradable && predicate(card))
            .OrderBy(GetStableCardPileKey, StringComparer.Ordinal)
            .ThenBy(card => card.Id.Entry, StringComparer.Ordinal)
            .ToList();

        return candidates.Count == 0 ? null : candidates[rng.NextInt(candidates.Count)];
    }

    private static string GetStableCardPileKey(CardModel card)
    {
        var pile = card.Pile;
        if (pile == null)
            return "none";

        var index = pile.Cards.IndexOf(card);
        return $"{pile.Type}:{index:D4}";
    }

    private static async Task<RelicModel> ObtainDuplicateInitialPointFallback(Player owner)
    {
        var eternalStarlight = await TokenEternalStarlight.GrantStacks(
            owner,
            DuplicateInitialPointFallbackEternalStarlightStacks);

        if (eternalStarlight != null)
            return eternalStarlight;

        return owner.GetRelic<TokenGoldInitialPoint>()!;
    }

    private static void GuardRewardSyncAllowed(string operation)
    {
        if (!RunManager.Instance.IsSinglePlayerOrFakeMultiplayer && CombatManager.Instance.IsInProgress)
            throw new InvalidOperationException($"Tried to sync {operation} during combat. Use deterministic combat actions instead.");
    }

    private static uint GetBestWeightedDeterministicScore(
        RelicModel relic,
        int weight,
        IReadOnlyList<object?> contextParts)
    {
        var relicId = (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
        var bestScore = uint.MaxValue;
        for (var i = 0; i < weight; i++)
        {
            var score = DeterministicMultiplayerChoiceHelper.RollDeterministically(
                0,
                int.MaxValue,
                contextParts.Concat([relicId, "weight", i]).ToArray());
            if ((uint)score < bestScore)
                bestScore = (uint)score;
        }

        return bestScore;
    }
}
