using STS2RitsuLib.Compat;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

internal static class OptionalModCompatRegistry
{
    private static readonly IReadOnlyList<OptionalCompatModInfo> SupportedMods =
    [
        new("Windchaser", "逐风者", "https://steamcommunity.com/sharedfiles/filedetails/?id=3747509821"),
        new("ManosabaLin", "魔女审判", "https://steamcommunity.com/sharedfiles/filedetails/?id=3747637918")
    ];

    public static IReadOnlyList<OptionalCompatModInfo> GetSupportedMods()
    {
        return SupportedMods;
    }

    public static bool IsModLoaded(string modId)
    {
        if (string.IsNullOrWhiteSpace(modId))
            return false;

        try
        {
            return RitsuModManager.IsModLoaded(modId);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] RitsuModManager compatibility load check failed for '{modId}': {ex.Message}");
            return false;
        }
    }
}

internal sealed record OptionalCompatModInfo(string ModId, string DisplayName, string? ReleasePageUrl);
