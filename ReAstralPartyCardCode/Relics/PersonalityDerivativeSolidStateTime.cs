using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public sealed class PersonalityDerivativeSolidStateTime : AstralPartyRelicModel
{
    private List<SolidStateTimeRecord> _records = [];

    [SavedProperty]
    private string AstralParty_PersonalityDerivativeSolidStateTimeRecordsJson
    {
        get => SolidStateTimeHelper.SerializeRecords(_records);
        set
        {
            _records = SolidStateTimeHelper.DeserializeRecords(value);
            SolidStateTimeHelper.NormalizeRecordThresholds(_records);
        }
    }

    [SavedProperty] public bool AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat { get; set; }
    [SavedProperty] public string AstralParty_PersonalityDerivativeSolidStateTimePrimaryRecordModelId { get; set; } = string.Empty;
    [SavedProperty] public int AstralParty_PersonalityDerivativeSolidStateTimeTouchSequence { get; set; }
    [SavedProperty] public int AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed { get; set; }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShouldReceiveCombatHooks => true;

    public override bool ShowCounter => Owner?.GetRelic<PersonMascotGirlMimiDoppelganger>() != null && GetPrimaryRecord() != null;

    public override int DisplayAmount => GetPrimaryRecord()?.Progress ?? 0;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => BuildHoverTips();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        _records.Clear();
        AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat = false;
        AstralParty_PersonalityDerivativeSolidStateTimePrimaryRecordModelId = string.Empty;
        AstralParty_PersonalityDerivativeSolidStateTimeTouchSequence = 0;
        AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed = 0;
        InvokeDisplayAmountChanged();
    }

    public override Task BeforeCombatStart()
    {
        AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat = false;
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || cardPlay.Card.Owner != Owner)
            return;
        if (!SolidStateTimeHelper.TryGetTrackedDeckPowerCard(Owner, cardPlay.Card, out var deckCard) || deckCard == null)
            return;

        var canonical = deckCard.CanonicalInstance ?? deckCard;
        var modelId = SolidStateTimeHelper.GetRecordModelId(canonical);
        var record = GetOrCreateRecord(modelId, deckCard.CurrentUpgradeLevel);

        record.UpgradeLevel = Math.Max(record.UpgradeLevel, deckCard.CurrentUpgradeLevel);
        record.LastTouchedSequence = ++AstralParty_PersonalityDerivativeSolidStateTimeTouchSequence;
        AstralParty_PersonalityDerivativeSolidStateTimePrimaryRecordModelId = record.ModelId;

        var gain = SolidStateTimeHelper.GetWeightedProgressGain(
            AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed);
        AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed++;
        record.Progress += gain;

        while (record.Progress >= record.NextThreshold)
        {
            record.Progress -= record.NextThreshold;
            record.Completions++;
            record.NextThreshold = SolidStateTimeHelper.GetThresholdForCompletionIndex(record.Completions);
            record.Progress = 0;

            await EventDeckCardHelper.RemoveCardFromRunDeck(Owner, deckCard, false);
            if (ReferenceEquals(cardPlay.Card, deckCard) || ReferenceEquals(cardPlay.Card.CanonicalInstance, canonical))
                await SolidStateTimeHelper.RemoveCardFromCombatPiles(cardPlay.Card);
        }

        Flash();
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState is not CombatState combatState)
            return;
        if (AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat)
            return;

        AstralParty_PersonalityDerivativeSolidStateTimePlayedThisCombat = true;
        var playableRecords = _records
            .Where(static record => record.Completions > 0)
            .OrderBy(static record => record.LastTouchedSequence)
            .ThenBy(static record => record.ModelId, StringComparer.Ordinal)
            .ToList();

        for (var recordIndex = 0; recordIndex < playableRecords.Count; recordIndex++)
        {
            var record = playableRecords[recordIndex];
            for (var playIndex = 0; playIndex < record.Completions; playIndex++)
            {
                var playbackCard = SolidStateTimeHelper.CreateCombatPlaybackCard(Owner, combatState, record);
                if (playbackCard == null)
                    continue;

                var target = SolidStateTimeHelper.ResolvePlaybackTarget(Owner, combatState, playbackCard, recordIndex, playIndex);
                if (playbackCard.TargetType == TargetType.AnyEnemy && target == null)
                {
                    MainFile.Logger.Warn($"[SolidStateTime] Skipped playback for '{playbackCard.Id.Entry}' because no valid enemy target was found.");
                    continue;
                }

                if (playbackCard.TargetType is not (TargetType.Self or TargetType.AnyAlly or TargetType.AnyPlayer or TargetType.AnyEnemy))
                {
                    MainFile.Logger.Warn($"[SolidStateTime] Skipped playback for unsupported target type '{playbackCard.TargetType}' on '{playbackCard.Id.Entry}'.");
                    continue;
                }

                playbackCard.SetToFreeThisTurn();
                await CardCmd.AutoPlay(choiceContext, playbackCard, target, AutoPlayType.Default, false, true);
                await SolidStateTimeHelper.RemoveCardFromCombatPiles(playbackCard);
            }
        }
    }

    private IEnumerable<IHoverTip> BuildHoverTips()
    {
        yield return BuildSummaryHoverTip();

        foreach (var record in _records
                     .OrderByDescending(static record => record.Completions)
                     .ThenByDescending(static record => record.LastTouchedSequence)
                     .ThenBy(static record => record.ModelId, StringComparer.Ordinal))
        {
            var canonical = SolidStateTimeHelper.TryGetCanonicalCard(record.ModelId);
            if (canonical == null)
                continue;

            yield return BuildEntryHoverTip(canonical, record);
        }
    }

    private HoverTip BuildSummaryHoverTip()
    {
        var bodyTemplate = new LocString("relics", $"{Id.Entry}.stats_description").GetRawText();
        var nextGain = AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed < 3
            ? SolidStateTimeHelper.GetWeightedProgressGain(
                AstralParty_PersonalityDerivativeSolidStateTimeWeightedRecordEventsConsumed)
            : 1;
        var body = string.Format(
            bodyTemplate,
            _records.Count,
            _records.Sum(static record => record.Completions),
            nextGain,
            _records.Count == 0
                ? new LocString("relics", $"{Id.Entry}.empty_entries").GetRawText()
                : string.Empty).TrimEnd();

        return new HoverTip(
            new LocString("relics", $"{Id.Entry}.stats_title"),
            body,
            GD.Load<Texture2D>(PackedIconPath));
    }

    private HoverTip BuildEntryHoverTip(CardModel canonicalCard, SolidStateTimeRecord record)
    {
        var bodyTemplate = new LocString("relics", $"{Id.Entry}.entry_description").GetRawText();
        var body = string.Format(
            bodyTemplate,
            record.Completions,
            record.Progress,
            record.NextThreshold);
        return new HoverTip(
            new LocString("relics", $"{Id.Entry}.stats_title"),
            $"{canonicalCard.Title}\n{body}",
            GD.Load<Texture2D>(PackedIconPath))
        {
            IsInstanced = true,
            Id = $"{Id.Entry}:{record.ModelId}:{record.Completions}:{record.Progress}:{record.NextThreshold}"
        };
    }

    private SolidStateTimeRecord? GetPrimaryRecord()
    {
        if (!string.IsNullOrWhiteSpace(AstralParty_PersonalityDerivativeSolidStateTimePrimaryRecordModelId))
        {
            var primary = _records.FirstOrDefault(record =>
                string.Equals(record.ModelId, AstralParty_PersonalityDerivativeSolidStateTimePrimaryRecordModelId, StringComparison.Ordinal));
            if (primary != null)
                return primary;
        }

        return _records
            .OrderByDescending(static record => record.LastTouchedSequence)
            .ThenBy(static record => record.ModelId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private SolidStateTimeRecord GetOrCreateRecord(string modelId, int upgradeLevel)
    {
        var existing = _records.FirstOrDefault(record => string.Equals(record.ModelId, modelId, StringComparison.Ordinal));
        if (existing != null)
            return existing;

        var created = new SolidStateTimeRecord
        {
            ModelId = modelId,
            UpgradeLevel = upgradeLevel,
            NextThreshold = SolidStateTimeHelper.GetThresholdForCompletionIndex(0),
            LastTouchedSequence = ++AstralParty_PersonalityDerivativeSolidStateTimeTouchSequence
        };
        _records.Add(created);
        return created;
    }
}
