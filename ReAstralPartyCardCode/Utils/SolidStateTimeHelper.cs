using System;
using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Saves;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class SolidStateTimeHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var canonicalPlayed = playedCard.CanonicalInstance ?? playedCard;
        var runDeckCards = EventDeckCardHelper.GetRunDeckCards(owner);
        deckCard = runDeckCards.FirstOrDefault(card =>
            ReferenceEquals(card, playedCard)
            || ReferenceEquals(card, canonicalPlayed)
            || ReferenceEquals(card.CanonicalInstance, canonicalPlayed));

        if (deckCard == null)
            return false;

        return owner.RunState.ContainsCard(deckCard);
    }

    public static string GetRecordModelId(CardModel card)
    {
        return (card.CanonicalInstance?.Id ?? card.Id).ToString();
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

    public static CardModel? CreateCombatPlaybackCard(Player owner, CombatState combatState, SolidStateTimeRecord record)
    {
        var canonical = TryGetCanonicalCard(record.ModelId);
        if (canonical == null || canonical.Type != CardType.Power)
            return null;

        var created = combatState.CreateCard(canonical.CanonicalInstance ?? canonical, owner);
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
            TargetType.AnyEnemy => ResolveDeterministicEnemyTarget(owner, combatState, playbackCard, recordIndex, playIndex),
            _ => null
        };
    }

    public static async Task RemoveCardFromCombatPiles(CardModel card)
    {
        if (card.Pile?.IsCombatPile != true)
            return;

        try
        {
            await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[SolidStateTime] Failed to remove combat card '{card.Id.Entry}': {ex.Message}");
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

    private static void ApplyUpgradeLevels(CardModel card, int upgrades)
    {
        var capped = Math.Clamp(upgrades, 0, card.MaxUpgradeLevel);
        for (var i = 0; i < capped; i++)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }
    }
}

internal sealed class SolidStateTimeRecord
{
    public string ModelId { get; set; } = string.Empty;
    public int UpgradeLevel { get; set; }
    public int Progress { get; set; }
    public int Completions { get; set; }
    public int NextThreshold { get; set; } = 3;
    public long LastTouchedSequence { get; set; }
}
