using MegaCrit.Sts2.Core.Modding;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Compat.Core;

internal static class OptionalModCompatRegistry
{
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
