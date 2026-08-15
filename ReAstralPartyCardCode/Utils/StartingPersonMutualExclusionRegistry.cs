using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StartingPersonMutualExclusionRegistry
{
    private const string DoppelgangerSuffix = "_doppelganger";

    public static IReadOnlyList<RelicModel> FilterOptions(IReadOnlyList<RelicModel> source)
    {
        if (source.Count <= 1)
            return source;

        var availableEntries = source
            .Select(GetCanonicalEntry)
            .ToHashSet(StringComparer.Ordinal);
        var filtered = new List<RelicModel>(source.Count);
        var chosenEntriesByGroup = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var relic in source)
        {
            var entry = GetCanonicalEntry(relic);
            var groupKey = GetDoppelgangerGroupKey(entry, availableEntries);
            if (groupKey == null)
            {
                filtered.Add(relic);
                continue;
            }

            if (!chosenEntriesByGroup.TryGetValue(groupKey, out var chosenEntry))
            {
                chosenEntriesByGroup[groupKey] = entry;
                filtered.Add(relic);
                continue;
            }

            if (string.Equals(chosenEntry, entry, StringComparison.Ordinal))
                filtered.Add(relic);
        }

        return filtered;
    }

    private static string GetCanonicalEntry(RelicModel relic)
    {
        return (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
    }

    private static string? GetDoppelgangerGroupKey(string entry, IReadOnlySet<string> availableEntries)
    {
        var baseEntry = entry.EndsWith(DoppelgangerSuffix, StringComparison.Ordinal)
            ? entry[..^DoppelgangerSuffix.Length]
            : entry;
        var doppelgangerEntry = $"{baseEntry}{DoppelgangerSuffix}";

        return availableEntries.Contains(baseEntry) && availableEntries.Contains(doppelgangerEntry)
            ? baseEntry
            : null;
    }
}
