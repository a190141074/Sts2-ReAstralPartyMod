using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal sealed record StartingPersonMutualExclusionGroupDefinition(
    string GroupKey,
    IReadOnlyList<string> RelicEntries)
{
    public bool Matches(RelicModel relic)
    {
        var entry = (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
        return RelicEntries.Any(candidate => string.Equals(candidate, entry, StringComparison.Ordinal));
    }
}

internal static class StartingPersonMutualExclusionRegistry
{
    private static readonly IReadOnlyList<StartingPersonMutualExclusionGroupDefinition> Definitions =
    [
        Create(
            "BIONIC_JASMINE",
            [
                "person_bionic_jasmine",
                "person_bionic_jasmine_doppelganger"
            ]),
        Create(
            "SLIME_LULU",
            [
                "person_slime_lulu",
                "person_slime_lulu_doppelganger"
            ]),
        Create(
            "PROPRIETRESS",
            [
                "person_proprietress",
                "person_proprietress_doppelganger"
            ])
    ];

    private static readonly IReadOnlyDictionary<string, StartingPersonMutualExclusionGroupDefinition> GroupByEntry =
        BuildGroupByEntry();

    public static IReadOnlyList<StartingPersonMutualExclusionGroupDefinition> All => Definitions;

    public static IReadOnlyList<RelicModel> FilterOptions(IReadOnlyList<RelicModel> source)
    {
        if (source.Count <= 1)
            return source;

        var filtered = new List<RelicModel>(source.Count);
        var chosenEntriesByGroup = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var relic in source)
        {
            var entry = (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
            if (!GroupByEntry.TryGetValue(entry, out var group))
            {
                filtered.Add(relic);
                continue;
            }

            if (!chosenEntriesByGroup.TryGetValue(group.GroupKey, out var chosenEntry))
            {
                chosenEntriesByGroup[group.GroupKey] = entry;
                filtered.Add(relic);
                continue;
            }

            if (string.Equals(chosenEntry, entry, StringComparison.Ordinal))
                filtered.Add(relic);
        }

        return filtered;
    }

    private static Dictionary<string, StartingPersonMutualExclusionGroupDefinition> BuildGroupByEntry()
    {
        var result = new Dictionary<string, StartingPersonMutualExclusionGroupDefinition>(StringComparer.Ordinal);
        foreach (var definition in Definitions)
        {
            foreach (var entry in definition.RelicEntries)
                result[entry] = definition;
        }

        return result;
    }

    private static StartingPersonMutualExclusionGroupDefinition Create(
        string groupKey,
        IReadOnlyList<string> relicEntries)
    {
        return new StartingPersonMutualExclusionGroupDefinition(groupKey, relicEntries);
    }
}
