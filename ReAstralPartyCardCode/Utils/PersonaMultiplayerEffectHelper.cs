using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Helpers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Online;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaMultiplayerEffectHelper
{
    private const int DuplicateInitialPointFallbackEternalStarlightStacks = 3;
    private static readonly AsyncLocal<int> DerivedSupportPowerDepth = new();

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

    public static bool IsResolvingDerivedSupportPower => DerivedSupportPowerDepth.Value > 0;

    public static async Task RunAsDerivedSupportPower(Func<Task> action)
    {
        DerivedSupportPowerDepth.Value++;
        try
        {
            await action();
        }
        finally
        {
            DerivedSupportPowerDepth.Value = Math.Max(0, DerivedSupportPowerDepth.Value - 1);
        }
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
        if (canonicalRelic.Id == ModelDb.GetId<TokenGoldInitialPoint>() &&
            owner.GetRelic<TokenGoldInitialPoint>() != null)
            return ObtainDuplicateInitialPointFallback(owner);

        return ObtainRelicDeterministicTracked(owner, canonicalRelic);
    }

    public static Task<RelicModel> ObtainRelicAsReward(Player owner, RelicModel relic)
    {
        var canonicalRelic = relic.CanonicalInstance ?? relic;
        if (CanUseRewardSynchronizer(owner, "relic reward"))
            RunManager.Instance?.RewardSynchronizer?.SyncLocalObtainedRelic(canonicalRelic.ToMutable());

        return ObtainRelicAsRewardTracked(owner, canonicalRelic);
    }

    public static void SyncGoldLostAsReward(Player owner, int goldLost)
    {
        if (CanUseRewardSynchronizer(owner, "gold loss"))
            RunManager.Instance?.RewardSynchronizer?.SyncLocalGoldLost(goldLost);
    }

    public static Task<RelicModel> ObtainRelicForMultiplayerSafeReward(Player owner, RelicModel relic)
    {
        if (CanUseRewardSynchronizer(owner, "safe relic reward"))
            return ObtainRelicAsReward(owner, relic);

        return ObtainRelicDeterministic(owner, relic);
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

    private static async Task<RelicModel> ObtainRelicDeterministicTracked(Player owner, RelicModel canonicalRelic)
    {
        MarkRelicAsSeenIfPossible(canonicalRelic);
        var obtained = await RelicCmd.Obtain(canonicalRelic.ToMutable(), owner);
        AstralTelemetry.RecordObtainedToken(owner, canonicalRelic);
        return obtained;
    }

    private static async Task<RelicModel> ObtainRelicAsRewardTracked(Player owner, RelicModel canonicalRelic)
    {
        MarkRelicAsSeenIfPossible(canonicalRelic);
        var obtained = await RelicCmd.Obtain(canonicalRelic.ToMutable(), owner);
        AstralTelemetry.RecordObtainedToken(owner, canonicalRelic);
        return obtained;
    }

    private static bool CanUseRewardSynchronizer(Player owner, string operation)
    {
        var runManager = RunManager.Instance;
        if (runManager == null)
        {
            MainFile.Logger.Warn($"Skipped reward sync for {operation}: RunManager unavailable.");
            return false;
        }

        if (!LocalContext.IsMe(owner))
            return false;

        if (!runManager.IsSinglePlayerOrFakeMultiplayer)
        {
            if (CombatManager.Instance?.IsInProgress == true)
            {
                MainFile.Logger.Warn(
                    $"Skipped reward sync for {operation}: multiplayer combat is in progress; deterministic path required.");
                return false;
            }

            if (owner.RunState == null)
            {
                MainFile.Logger.Warn(
                    $"Skipped reward sync for {operation}: no active run state for owner {owner.NetId}.");
                return false;
            }
        }

        if (runManager.RewardSynchronizer == null)
        {
            MainFile.Logger.Warn($"Skipped reward sync for {operation}: RewardSynchronizer unavailable.");
            return false;
        }

        return true;
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

    private static void MarkRelicAsSeenIfPossible(RelicModel relic)
    {
        SaveManager.Instance?.MarkRelicAsSeen(relic.CanonicalInstance ?? relic);
    }
}
