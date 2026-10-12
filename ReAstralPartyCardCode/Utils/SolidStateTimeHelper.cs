using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SolidStateTimeHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly MethodInfo CardOnPlayMethod =
        typeof(CardModel).GetMethod("OnPlay", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException("CardModel.OnPlay was not found.");

    public static string SerializeRecords(IEnumerable<SolidStateTimeRecord> records)
    {
        var ordered = records
            .OrderBy(static record => record.ModelId, StringComparer.Ordinal)
            .ToList();
        return JsonSerializer.Serialize(ordered, JsonOptions);
    }

    public static List<SolidStateTimeRecord> DeserializeRecords(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<SolidStateTimeRecord>>(value, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static bool TryGetTrackedDeckPowerCard(Player? owner, CardModel? playedCard, out CardModel? deckCard)
    {
        deckCard = null;
        if (owner?.RunState == null || playedCard == null)
            return false;
        if (playedCard.Owner != owner || playedCard.Type != CardType.Power)
            return false;

        if (playedCard.DeckVersion is { } deckVersion
            && deckVersion.Owner == owner
            && deckVersion.Type == CardType.Power
            && owner.RunState.ContainsCard(deckVersion))
        {
            deckCard = deckVersion;
            return true;
        }

        var playedStateKey = GetRecordStateKey(playedCard);
        var runDeckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        deckCard = runDeckCards.FirstOrDefault(card =>
            card.Owner == owner
            && card.Type == CardType.Power
            && string.Equals(GetRecordStateKey(card), playedStateKey, StringComparison.Ordinal));

        if (deckCard == null)
            return false;

        return owner.RunState.ContainsCard(deckCard);
    }

    public static string GetRecordModelId(CardModel card)
    {
        return (card.CanonicalInstance?.Id ?? card.Id).ToString();
    }

    public static string GetRecordStateKey(CardModel card)
    {
        var canonicalId = GetRecordModelId(card);
        var madScienceCardType = card is MadScience madScience
            ? ((int)madScience.TinkerTimeType).ToString()
            : string.Empty;
        var madScienceRider = card is MadScience madScienceWithRider
            ? ((int)madScienceWithRider.TinkerTimeRider).ToString()
            : string.Empty;
        return string.Join("|", canonicalId, card.CurrentUpgradeLevel, madScienceCardType, madScienceRider);
    }

    public static string GetRecordStateKey(SolidStateTimeRecord record)
    {
        return string.Join(
            "|",
            record.ModelId,
            record.UpgradeLevel,
            record.MadScienceCardType?.ToString() ?? string.Empty,
            record.MadScienceRider?.ToString() ?? string.Empty);
    }

    public static bool MatchesRecordState(SolidStateTimeRecord record, CardModel card)
    {
        return string.Equals(GetRecordStateKey(record), GetRecordStateKey(card), StringComparison.Ordinal);
    }

    public static CardModel? TryGetCanonicalCard(string serializedModelId)
    {
        try
        {
            return ModelDb.GetById<CardModel>(ModelId.Deserialize(serializedModelId));
        }
        catch
        {
            return null;
        }
    }

    public static int GetWeightedProgressGain(int weightedRecordEventsConsumed)
    {
        return weightedRecordEventsConsumed switch
        {
            0 => 3,
            1 or 2 => 2,
            _ => 1
        };
    }

    public static string BuildProgressText(SolidStateTimeRecord record)
    {
        return $"{record.Completions}+{record.Progress}/{record.NextThreshold}";
    }

    public static int GetThresholdForCompletionIndex(int completions)
    {
        if (completions <= 0)
            return 3;
        if (completions == 1)
            return 3;
        if (completions == 2)
            return 6;
        if (completions == 3)
            return 9;
        if (completions == 4)
            return 15;

        var previous = 9;
        var current = 15;
        for (var index = 5; index <= completions; index++)
        {
            var next = previous + current;
            previous = current;
            current = next;
        }

        return current;
    }

    public static void NormalizeRecordThresholds(IEnumerable<SolidStateTimeRecord> records)
    {
        foreach (var record in records)
            record.NextThreshold = GetThresholdForCompletionIndex(record.Completions);
    }

    public static IReadOnlyList<CardModel> GetRewardablePowerCardsIgnoringCharacterPool(Player owner)
    {
        return ModelDb.AllCards
            .Where(static card => card.Type == CardType.Power)
            .Where(static card => card.Rarity != CardRarity.Token)
            .Where(static card => card.ShouldShowInCardLibrary)
            .Where(static card => card.GetType().Assembly == typeof(CardModel).Assembly)
            .GroupBy(static card => card.CanonicalInstance?.Id ?? card.Id)
            .Select(static group => group.First().CanonicalInstance ?? group.First())
            .OrderBy(static card => card.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }

    public static CardModel? CreatePreviewCard(SolidStateTimeRecord record)
    {
        var canonical = TryGetCanonicalCard(record.ModelId);
        if (canonical == null || !IsStoredAsPowerCard(canonical, record))
            return null;

        var preview = (canonical.CanonicalInstance ?? canonical).ToMutable();
        ApplyStoredCardState(preview, record);
        ApplyUpgradeLevels(preview, record.UpgradeLevel);
        return preview;
    }

    public static CardModel? CreateCombatPlaybackCard(Player owner, CombatState combatState,
        SolidStateTimeRecord record)
    {
        var canonical = TryGetCanonicalCard(record.ModelId);
        if (canonical == null || !IsStoredAsPowerCard(canonical, record))
            return null;

        var created = combatState.CreateCard(canonical.CanonicalInstance ?? canonical, owner);
        ApplyStoredCardState(created, record);
        ApplyUpgradeLevels(created, record.UpgradeLevel);
        SaveManager.Instance?.MarkCardAsSeen(created.CanonicalInstance ?? created);
        return created;
    }

    public static Creature? ResolvePlaybackTarget(
        Player owner,
        CombatState combatState,
        CardModel playbackCard,
        int recordIndex,
        int playIndex)
    {
        return playbackCard.TargetType switch
        {
            TargetType.Self or TargetType.AnyAlly or TargetType.AnyPlayer => owner.Creature,
            TargetType.AnyEnemy => ResolveDeterministicEnemyTarget(owner, combatState, playbackCard, recordIndex,
                playIndex),
            _ => null
        };
    }

    public static async Task RemoveCardFromCombatPiles(CardModel card)
    {
        if (card.Pile?.IsCombatPile != true)
            return;

        try
        {
            await CardPileCmd.RemoveFromCombat(card, true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[SolidStateTime] Failed to remove combat card '{card.Id.Entry}': {ex.Message}");
        }
    }

    public static async Task PlayStoredPowerDirectly(PlayerChoiceContext choiceContext, CardModel card,
        Creature? target)
    {
        var addedToTemporaryPlayPile = false;
        if (card.Pile == null)
        {
            await CardPileCmd.Add(card, PileType.Play, skipVisuals: true);
            addedToTemporaryPlayPile = card.Pile?.Type == PileType.Play;
            if (!addedToTemporaryPlayPile)
            {
                MainFile.Logger.Warn($"[SolidStateTime] Skipped stored power without combat pile: card={card.Id}");
                return;
            }
        }

        var cardPlay = new CardPlay
        {
            Card = card,
            Target = target,
            ResultPile = PileType.None,
            Resources = new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = 0,
                StarsSpent = 0,
                StarValue = 0
            },
            IsAutoPlay = true,
            PlayIndex = 0,
            PlayCount = 1
        };

        choiceContext.PushModel(card);
        try
        {
            await (Task)GetOnPlayMethod(card).Invoke(card, [choiceContext, cardPlay])!;

            if (!card.Owner.Creature.IsDead)
                card.InvokeExecutionFinished();
        }
        finally
        {
            choiceContext.PopModel(card);
            if (addedToTemporaryPlayPile && card.Pile?.IsCombatPile == true)
                await CardPileCmd.RemoveFromCombat(card, true);
        }
    }

    private static Creature? ResolveDeterministicEnemyTarget(
        Player owner,
        CombatState combatState,
        CardModel playbackCard,
        int recordIndex,
        int playIndex)
    {
        var enemies = combatState
            .GetOpponentsOf(owner.Creature)
            .Where(static creature => creature.IsAlive)
            .OrderBy(static creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(static creature => creature.ModelId.ToString(), StringComparer.Ordinal)
            .ThenBy(static creature => creature.SlotName ?? string.Empty, StringComparer.Ordinal)
            .ToList();
        if (enemies.Count == 0)
            return null;

        var index = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            enemies.Count,
            MainFile.ModId,
            nameof(PersonalityDerivativeSolidStateTime),
            playbackCard.Id.Entry,
            owner.NetId,
            combatState.RoundNumber,
            recordIndex,
            playIndex);
        return enemies[index];
    }

    private static bool IsStoredAsPowerCard(CardModel? canonical, SolidStateTimeRecord record)
    {
        if (canonical is MadScience)
            return record.GetMadScienceCardType() == CardType.Power;

        return canonical?.Type == CardType.Power;
    }

    private static void ApplyUpgradeLevels(CardModel card, int upgrades)
    {
        var capped = Math.Clamp(upgrades, 0, card.MaxUpgradeLevel);
        for (var i = 0; i < capped; i++)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
    }

    private static void ApplyStoredCardState(CardModel card, SolidStateTimeRecord record)
    {
        if (card is MadScience madScience)
        {
            madScience.TinkerTimeType = record.GetMadScienceCardType();
            madScience.TinkerTimeRider = record.GetMadScienceRider();
        }
    }

    private static MethodInfo GetOnPlayMethod(CardModel card)
    {
        var type = card.GetType();
        while (type != null)
        {
            var method = type.GetMethod("OnPlay",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (method != null)
                return method;

            type = type.BaseType;
        }

        return CardOnPlayMethod;
    }
}

internal sealed class SolidStateTimeRecord
{
    public string ModelId { get; set; } = string.Empty;
    public int UpgradeLevel { get; set; }
    public int? MadScienceCardType { get; set; }
    public int? MadScienceRider { get; set; }
    public int Progress { get; set; }
    public int Completions { get; set; }
    public int NextThreshold { get; set; } = 3;
    public long LastTouchedSequence { get; set; }

    public CardType GetMadScienceCardType()
    {
        return MadScienceCardType.HasValue
            ? (CardType)MadScienceCardType.Value
            : CardType.Power;
    }

    public TinkerTime.RiderEffect GetMadScienceRider()
    {
        return MadScienceRider.HasValue
            ? (TinkerTime.RiderEffect)MadScienceRider.Value
            : TinkerTime.RiderEffect.None;
    }
}
