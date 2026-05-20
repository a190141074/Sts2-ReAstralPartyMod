using MegaCrit.Sts2.Core.Modding;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

internal static class OptionalModCompatRegistry
{
    private static readonly IReadOnlyList<OptionalCompatModInfo> SupportedMods =
    [
        new("《风行者》", "Windchaser", "https://www.bilibili.com/video/BV145RpBCEG4/")
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
            return ModManager.GetLoadedMods().Any(mod =>
                string.Equals(mod.manifest?.id, modId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"[{MainFile.ModId}] Optional mod compatibility load check failed for '{modId}': {ex.Message}");
            return false;
        }
    }
}

internal sealed record OptionalCompatModInfo(string ModId, string DisplayName, string? ReleasePageUrl);
