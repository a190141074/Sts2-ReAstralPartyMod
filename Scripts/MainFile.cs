using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AstralPartyMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "AstralPartyMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        var buildMarker =
            typeof(MainFile).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
            ?? typeof(MainFile).Assembly.GetName().Version?.ToString()
            ?? "unknown";
        Logger.Info($"AstralPartyMod initialized | build={buildMarker}");
        LogLoadedArtifacts(buildMarker);
    }

    private static void LogLoadedArtifacts(string buildMarker)
    {
        try
        {
            var assemblyPath = typeof(MainFile).Assembly.Location;
            var modDirectory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
            var pckPath = Path.Combine(modDirectory, "AstralPartyMod.pck");
            var manifestPath = Path.Combine(modDirectory, "AstralPartyMod.json");

            Logger.Info(
                $"AstralPartyMod artifacts | build={buildMarker} | dll={assemblyPath} | dll_sha256={ComputeSha256(assemblyPath)}"
            );

            if (File.Exists(pckPath))
            {
                Logger.Info(
                    $"AstralPartyMod artifacts | pck={pckPath} | pck_sha256={ComputeSha256(pckPath)}"
                );
            }
            else
            {
                Logger.Warn($"AstralPartyMod artifacts | pck missing at {pckPath}");
            }

            if (File.Exists(manifestPath))
            {
                Logger.Info(
                    $"AstralPartyMod artifacts | manifest={manifestPath} | manifest_sha256={ComputeSha256(manifestPath)}"
                );
            }
            else
            {
                Logger.Warn($"AstralPartyMod artifacts | manifest missing at {manifestPath}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to log AstralPartyMod artifact hashes: {ex}");
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}
