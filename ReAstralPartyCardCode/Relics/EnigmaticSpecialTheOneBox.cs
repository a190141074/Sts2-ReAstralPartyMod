using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Rewards;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(EventRelicPool))]
public class EnigmaticSpecialTheOneBox : AstralPartyRelicModel
{
    private Dictionary<string, int> _storedCounts = new(StringComparer.Ordinal);

    [SavedProperty]
    private string AstralParty_EnigmaticSpecialTheOneBoxStoredCountsSerialized
    {
        get => JsonSerializer.Serialize(
            _storedCounts
                .Where(static pair => pair.Value > 0)
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal));
        set => _storedCounts = DeserializeCounts(value);
    }

    protected override string RelicId => "enigmatic_special_the_one_box";

    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => BuildHoverTips();

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        await AbsorbVisibleMaterialsAsync();
    }

    public int GetStoredAmount(EnigmaticUniqueMaterialKind kind)
    {
        return !EnigmaticTheOneBoxHelper.IsBoxedKind(kind)
            ? 0
            : _storedCounts.GetValueOrDefault(GetStorageKey(kind), 0);
    }

    public void AddStoredMaterial(EnigmaticUniqueMaterialKind kind, int amount)
    {
        if (amount <= 0 || !EnigmaticTheOneBoxHelper.IsBoxedKind(kind))
            return;

        var key = GetStorageKey(kind);
        _storedCounts[key] = Math.Max(0, _storedCounts.GetValueOrDefault(key, 0) + amount);
        InvokeDisplayAmountChanged();
    }

    public Task ConsumeStoredMaterialAsync(EnigmaticUniqueMaterialKind kind, int amount)
    {
        if (amount <= 0 || !EnigmaticTheOneBoxHelper.IsBoxedKind(kind))
            return Task.CompletedTask;

        var key = GetStorageKey(kind);
        var updated = Math.Max(0, _storedCounts.GetValueOrDefault(key, 0) - amount);
        if (updated <= 0)
            _storedCounts.Remove(key);
        else
            _storedCounts[key] = updated;

        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }

    private IEnumerable<IHoverTip> BuildHoverTips()
    {
        yield return AstralKeywords.CreateHoverTip(AstralKeywords.AstralUniqueMaterialId);
        yield return BuildSummaryHoverTip();

        foreach (var entry in GetStoredEntries())
            yield return BuildEntryHoverTip(entry);
    }

    private HoverTip BuildSummaryHoverTip()
    {
        var entries = GetStoredEntries().ToList();
        var bodyTemplate = new LocString("relics", $"{Id.Entry}.stats_description").GetRawText();
        var body = string.Format(
            bodyTemplate,
            entries.Count,
            entries.Sum(static entry => entry.Count),
            entries.Count == 0
                ? new LocString("relics", $"{Id.Entry}.empty_entries").GetRawText()
                : string.Empty).TrimEnd();

        return new HoverTip(
            new LocString("relics", $"{Id.Entry}.stats_title"),
            body,
            GD.Load<Texture2D>(PackedIconPath));
    }

    private HoverTip BuildEntryHoverTip((EnigmaticUniqueMaterialKind Kind, RelicModel Relic, int Count) entry)
    {
        var bodyTemplate = new LocString("relics", $"{Id.Entry}.entry_description").GetRawText();
        return new HoverTip(
            entry.Relic.Title,
            string.Format(bodyTemplate, entry.Count),
            entry.Relic.Icon)
        {
            IsInstanced = true,
            Id = $"{Id.Entry}:{entry.Kind}:{entry.Count}"
        };
    }

    private IEnumerable<(EnigmaticUniqueMaterialKind Kind, RelicModel Relic, int Count)> GetStoredEntries()
    {
        foreach (var kind in Enum.GetValues<EnigmaticUniqueMaterialKind>())
        {
            if (!EnigmaticTheOneBoxHelper.IsBoxedKind(kind))
                continue;

            var count = GetStoredAmount(kind);
            if (count <= 0)
                continue;

            yield return (kind, EnigmaticRewardRegistry.GetConfig(kind).Relic, count);
        }
    }

    private async Task AbsorbVisibleMaterialsAsync()
    {
        if (Owner == null)
            return;

        var materials = EnigmaticTheOneBoxHelper.GetVisibleBoxedMaterials(Owner);
        foreach (var material in materials)
        {
            if (!EnigmaticTheOneBoxHelper.TryGetBoxedKind(material, out var kind))
                continue;

            AddStoredMaterial(kind, material.SynthesisAmount);
            await RelicCmd.Remove(material);
        }
    }

    private static string GetStorageKey(EnigmaticUniqueMaterialKind kind)
    {
        return EnigmaticRewardRegistry.GetConfig(kind).RelicIdEntry;
    }

    private static Dictionary<string, int> DeserializeCounts(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(value)
                   ?.Where(static pair => pair.Value > 0 && !string.IsNullOrWhiteSpace(pair.Key))
                   .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal)
                   ?? new Dictionary<string, int>(StringComparer.Ordinal);
        }
        catch
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }
    }
}
