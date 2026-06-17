namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

internal sealed record SunsetGlowElementSelectionResult(
    string BranchId,
    int SelectedIndex,
    IReadOnlyList<SunsetGlowElementSelectionOption> FinalOptions);
