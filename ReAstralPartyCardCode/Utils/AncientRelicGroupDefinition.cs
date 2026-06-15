using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public sealed record AncientRelicGroupDefinition(
    string GroupKey,
    string TitleLocKey,
    string? DescriptionLocKey,
    string RepresentativeRelicId,
    int DisplayOrder,
    IReadOnlyList<string> RelicIds,
    IReadOnlyList<string> RelicIdPrefixes)
{
    public LocString TitleLocString => new("relics", TitleLocKey);
    public LocString? DescriptionLocString => DescriptionLocKey == null ? null : new("relics", DescriptionLocKey);

    public bool Matches(RelicModel relic)
    {
        var relicId = (relic.CanonicalInstance?.Id ?? relic.Id).Entry;
        if (RelicIds.Any(candidate => string.Equals(candidate, relicId, StringComparison.Ordinal)))
            return true;

        return RelicIdPrefixes.Any(prefix => relicId.StartsWith(prefix, StringComparison.Ordinal));
    }
}

public sealed record AncientRelicGroupOption(
    AncientRelicGroupDefinition Definition,
    RelicModel RepresentativeRelic,
    IReadOnlyList<RelicModel> Relics)
{
    public string GroupKey => Definition.GroupKey;
    public string Title => Definition.TitleLocString.GetFormattedText();
    public string Description => Definition.DescriptionLocString?.GetFormattedText() ?? string.Empty;
    public int DisplayOrder => Definition.DisplayOrder;
}
