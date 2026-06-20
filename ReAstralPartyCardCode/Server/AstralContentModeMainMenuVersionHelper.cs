using System.Reflection;
using System.Text.Json;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Server;

internal static class AstralContentModeMainMenuVersionHelper
{
    private static string? _cachedVersion;

    public static string GetModVersion()
    {
        if (!string.IsNullOrWhiteSpace(_cachedVersion))
            return _cachedVersion!;

        try
        {
            var manifestPath = TryGetModManifestPath();
            if (!string.IsNullOrWhiteSpace(manifestPath) && File.Exists(manifestPath))
            {
                using var stream = File.OpenRead(manifestPath);
                using var document = JsonDocument.Parse(stream);
                if (document.RootElement.TryGetProperty("version", out var versionElement))
                {
                    var manifestVersion = versionElement.GetString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(manifestVersion))
                    {
                        _cachedVersion = manifestVersion;
                        return manifestVersion;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"[astral_content_mode_main_menu_version] Failed to read manifest version: {ex.Message}");
        }

        _cachedVersion = typeof(MainFile).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? typeof(MainFile).Assembly.GetName().Version?.ToString()
                         ?? "unknown";
        return _cachedVersion;
    }

    private static string? TryGetModManifestPath()
    {
        try
        {
            var assemblyPath = typeof(MainFile).Assembly.Location;
            var modDirectory = Path.GetDirectoryName(assemblyPath);
            if (string.IsNullOrWhiteSpace(modDirectory))
                return null;

            var candidates = new[]
            {
                Path.Combine(modDirectory, "ReAstralPartyMod.json"),
                Path.Combine(Directory.GetParent(modDirectory)?.FullName ?? modDirectory, "ReAstralPartyMod.json")
            };

            return candidates.FirstOrDefault(File.Exists);
        }
        catch
        {
            return null;
        }
    }
}
