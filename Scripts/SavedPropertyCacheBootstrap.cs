using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;

namespace ReAstralPartyMod;

internal static class SavedPropertyCacheBootstrap
{
    private static bool _verificationScheduled;

    public static void ScheduleVerification(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (_verificationScheduled)
            return;

        _verificationScheduled = true;
        RitsuLibFramework.SubscribeLifecycle<ModelRegistryInitializedEvent>(
            _ => LogVerification(assembly),
            replayCurrentState: true);
    }

    private static void LogVerification(Assembly assembly)
    {
        try
        {
            var result = InspectAssemblyCache(assembly);
            MainFile.Logger.Info(
                $"{MainFile.ModId} saved properties cache verification | discovered_types={result.DiscoveredTypeCount} | cached_types={result.CachedTypeCount} | missing_types={result.MissingTypeCount} | discovered_properties={result.DiscoveredPropertyCount} | total_property_names={result.TotalPropertyNameCount} | net_id_bits={result.NetIdBitSize} | expected_net_id_bits={result.ExpectedNetIdBitSize} | fingerprint={result.Fingerprint}");

            if (result.MissingTypeCount > 0 || result.NetIdBitSize != result.ExpectedNetIdBitSize)
                MainFile.Logger.Warn(
                    $"{MainFile.ModId} saved properties cache verification warning | missing_types={result.MissingTypeCount} | net_id_bits={result.NetIdBitSize} | expected_net_id_bits={result.ExpectedNetIdBitSize}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn(
                $"{MainFile.ModId} saved properties cache verification failed: {ex.Message}");
        }
    }

    public static SavedPropertyCacheVerificationResult InspectAssemblyCache(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var cachedTypeCount = 0;
        var discoveredTypeCount = 0;
        var discoveredPropertyCount = 0;

        var modelTypes = DeterministicTypeCatalog
            .GetAssignableTypes<AbstractModel>(assembly, HasSavedProperties)
            .ToArray();
        var fingerprint = ComputeFingerprint(modelTypes);

        foreach (var modelType in modelTypes)
        {
            discoveredTypeCount++;
            discoveredPropertyCount += GetSavedPropertyCount(modelType);

            if (SavedPropertiesTypeCache.GetJsonPropertiesForType(modelType) != null)
                cachedTypeCount++;
        }

        var totalPropertyNameCount = GetPropertyNameCount();
        var expectedNetIdBitSize = ComputeNetIdBitSize(totalPropertyNameCount);
        return new SavedPropertyCacheVerificationResult(
            discoveredTypeCount,
            cachedTypeCount,
            discoveredTypeCount - cachedTypeCount,
            discoveredPropertyCount,
            totalPropertyNameCount,
            SavedPropertiesTypeCache.NetIdBitSize,
            expectedNetIdBitSize,
            fingerprint);
    }

    private static bool HasSavedProperties(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Any(static property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }

    private static int GetSavedPropertyCount(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Count(static property => property.GetCustomAttribute<SavedPropertyAttribute>() != null);
    }

    private static int GetPropertyNameCount()
    {
        var field = typeof(SavedPropertiesTypeCache).GetField(
            "_netIdToPropertyNameMap",
            BindingFlags.Static | BindingFlags.NonPublic);
        var propertyNames = (List<string>?)field?.GetValue(null)
                            ?? throw new InvalidOperationException(
                                "Failed to read SavedPropertiesTypeCache property name map.");

        return propertyNames.Count;
    }

    private static int ComputeNetIdBitSize(int count)
    {
        return count <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(count));
    }

    private static string ComputeFingerprint(IEnumerable<Type> modelTypes)
    {
        var builder = new StringBuilder();

        foreach (var modelType in modelTypes)
        {
            builder.Append(modelType.FullName);
            builder.Append('|');

            foreach (var property in GetSavedProperties(modelType))
            {
                builder.Append(property.Name);
                builder.Append(':');
                builder.Append(property.PropertyType.FullName);
                builder.Append(';');
            }

            builder.AppendLine();
        }

        var bytes = Encoding.UTF8.GetBytes(builder.ToString());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static IReadOnlyList<PropertyInfo> GetSavedProperties(Type modelType)
    {
        return modelType
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.GetCustomAttribute<SavedPropertyAttribute>() != null)
            .OrderBy(static property => property.Name, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed record SavedPropertyCacheVerificationResult(
    int DiscoveredTypeCount,
    int CachedTypeCount,
    int MissingTypeCount,
    int DiscoveredPropertyCount,
    int TotalPropertyNameCount,
    int NetIdBitSize,
    int ExpectedNetIdBitSize,
    string Fingerprint);
