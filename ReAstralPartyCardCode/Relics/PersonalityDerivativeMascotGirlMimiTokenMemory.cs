using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Godot;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class PersonalityDerivativeMascotGirlMimiTokenMemory : AstralPartyRelicModel
{
    private const int RewardThreshold = 3;
    private const int DrawsPerTokenAbilityChoice = 25;

    private Dictionary<string, int> _temporaryTokenGainCounts = new(StringComparer.Ordinal);
    private HashSet<string> _readyRewardTokenIds = new(StringComparer.Ordinal);


    [SavedProperty]
    private string AstralParty_PersonalityDerivativeMascotGirlMimiTokenMemoryCountsJson
    {
        get => JsonSerializer.Serialize(_temporaryTokenGainCounts);
        set => _temporaryTokenGainCounts = DeserializeCounts(value);
    }

    [SavedProperty]
    private string AstralParty_PersonalityDerivativeMascotGirlMimiTokenMemoryReadyRewardsJson
    {
        get => JsonSerializer.Serialize(_readyRewardTokenIds.OrderBy(id => id, StringComparer.Ordinal).ToArray());
        set => _readyRewardTokenIds = DeserializeReadyRewards(value);
    }

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool ShowCounter => Owner?.GetRelic<PersonMascotGirlMimi>() != null;

    public override int DisplayAmount => GetCurrentDrawProgress();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => BuildHoverTips();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        _temporaryTokenGainCounts.Clear();
        _readyRewardTokenIds.Clear();
        InvokeDisplayAmountChanged();
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (Owner == null)
            return Task.CompletedTask;

        CleanupOwnedRewardEntries();

        foreach (var tokenRelicId in GetReadyRewardTokenIds())
        {
            var tokenRelic = ModelDb.GetById<RelicModel>(tokenRelicId);
            if (MascotGirlMimiTokenMemoryHelper.PlayerOwnsTokenRelic(Owner, tokenRelicId))
                continue;
            if (PersonaMultiplayerEffectHelper.IsRelicBannedForOwner(Owner, tokenRelic))
                continue;

            room.AddExtraReward(Owner, new RelicReward(tokenRelic.ToMutable(), Owner));
        }

        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    public void RecordTemporaryTokenGain(ModelId tokenRelicId)
    {
        if (tokenRelicId == ModelId.none)
            return;

        var tokenKey = tokenRelicId.ToString();
        _temporaryTokenGainCounts.TryGetValue(tokenKey, out var currentCount);
        currentCount++;
        _temporaryTokenGainCounts[tokenKey] = currentCount;

        if (currentCount >= RewardThreshold &&
            !MascotGirlMimiTokenMemoryHelper.PlayerOwnsTokenRelic(Owner, tokenRelicId))
            _readyRewardTokenIds.Add(tokenKey);

        InvokeDisplayAmountChanged();
    }

    public void RefreshProgressDisplay()
    {
        InvokeDisplayAmountChanged();
    }

    private IEnumerable<IHoverTip> BuildHoverTips()
    {
        yield return BuildSummaryHoverTip();

        foreach (var entry in GetRecordedEntries())
            yield return BuildEntryHoverTip(entry);
    }

    private HoverTip BuildSummaryHoverTip()
    {
        var bodyTemplate =
            new LocString("relics", $"{Id.Entry}.stats_description").GetRawText();
        var body = string.Format(
            bodyTemplate,
            _temporaryTokenGainCounts.Count,
            GetReadyRewardCount(),
            _temporaryTokenGainCounts.Count == 0
                ? new LocString("relics", $"{Id.Entry}.empty_entries").GetRawText()
                : string.Empty).TrimEnd();

        return new HoverTip(
            new LocString("relics", $"{Id.Entry}.stats_title"),
            body,
            GD.Load<Texture2D>(PackedIconPath));
    }

    private HoverTip BuildEntryHoverTip(
        (RelicModel TokenRelic, int Count, bool IsReadyReward, bool IsOwned) entry)
    {
        var bodyTemplate = new LocString("relics", $"{Id.Entry}.entry_description").GetRawText();
        var body = $"记忆次数：{string.Format(bodyTemplate, entry.Count, RewardThreshold)}";

        return new HoverTip(entry.TokenRelic.Title, body, entry.TokenRelic.Icon)
        {
            IsInstanced = true,
            Id = $"{Id.Entry}:{entry.TokenRelic.Id}:{entry.Count}:{entry.IsReadyReward}"
        };
    }

    private IEnumerable<(RelicModel TokenRelic, int Count, bool IsReadyReward, bool IsOwned)> GetRecordedEntries()
    {
        CleanupOwnedRewardEntries();

        return _temporaryTokenGainCounts
            .Select(entry => (TokenRelic: TryGetTokenRelic(entry.Key), Count: entry.Value, Key: entry.Key))
            .Where(entry => entry.TokenRelic != null)
            .Select(entry => (
                TokenRelic: entry.TokenRelic!,
                Count: entry.Count,
                IsReadyReward: _readyRewardTokenIds.Contains(entry.Key),
                IsOwned: MascotGirlMimiTokenMemoryHelper.PlayerOwnsTokenRelic(Owner, entry.TokenRelic!.Id)))
            .OrderByDescending(entry => entry.Count)
            .ThenBy(entry => entry.TokenRelic.Title.GetRawText(), StringComparer.Ordinal);
    }

    private IReadOnlyList<ModelId> GetReadyRewardTokenIds()
    {
        CleanupOwnedRewardEntries();

        return _readyRewardTokenIds
            .Select(TryDeserializeModelId)
            .Where(modelId => modelId != null)
            .Select(modelId => modelId!)
            .ToList();
    }

    private int GetReadyRewardCount()
    {
        return GetReadyRewardTokenIds().Count;
    }

    private int GetCurrentDrawProgress()
    {
        var mimi = Owner?.GetRelic<PersonMascotGirlMimi>();
        if (mimi == null)
            return 0;

        return Math.Clamp(
            mimi.AstralParty_PersonMascotGirlMimiProductRestockingDrawProgress,
            0,
            DrawsPerTokenAbilityChoice);
    }

    private void CleanupOwnedRewardEntries()
    {
        if (Owner == null || _readyRewardTokenIds.Count == 0)
            return;

        _readyRewardTokenIds.RemoveWhere(tokenKey =>
        {
            var tokenRelicId = TryDeserializeModelId(tokenKey);
            return tokenRelicId != null && MascotGirlMimiTokenMemoryHelper.PlayerOwnsTokenRelic(Owner, tokenRelicId);
        });
    }

    private static RelicModel? TryGetTokenRelic(string tokenKey)
    {
        var tokenRelicId = TryDeserializeModelId(tokenKey);
        if (tokenRelicId == null)
            return null;

        try
        {
            return ModelDb.GetById<RelicModel>(tokenRelicId);
        }
        catch
        {
            return null;
        }
    }

    private static ModelId? TryDeserializeModelId(string tokenKey)
    {
        try
        {
            return ModelId.Deserialize(tokenKey);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, int> DeserializeCounts(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(value)
                   ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }

    private static HashSet<string> DeserializeReadyRewards(string value)
    {
        try
        {
            var ids = JsonSerializer.Deserialize<string[]>(value) ?? [];
            return ids.ToHashSet(StringComparer.Ordinal);
        }
        catch
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
