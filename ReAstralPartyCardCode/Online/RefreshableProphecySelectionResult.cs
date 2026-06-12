using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed class RefreshableProphecySelectionResult
{
    public required ProphecySoulDevourKind SelectedProphecy { get; init; }
    public required int SelectedIndex { get; init; }
    public required int RefreshCost { get; init; }
    public required int RefreshCount { get; init; }
    public required IReadOnlyList<int> RerollHistory { get; init; }
    public required IReadOnlyList<ProphecySoulDevourKind> FinalOptions { get; init; }
}
