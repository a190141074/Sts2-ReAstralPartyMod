using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ReAstralPartyMod;

internal static class SavedPropertyCacheBootstrap
{
    public static SavedPropertyBootstrapResult PreloadFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var injectedTypeCount = 0;
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
                continue;

            SavedPropertiesTypeCache.InjectTypeIntoCache(modelType);
            injectedTypeCount++;
        }

        var totalPropertyNameCount = RecomputeNetIdBitSize();
        return new SavedPropertyBootstrapResult(
            discoveredTypeCount,
            injectedTypeCount,
            discoveredPropertyCount,
            totalPropertyNameCount,
            SavedPropertiesTypeCache.NetIdBitSize,
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

    private static int RecomputeNetIdBitSize()
    {
        var field = typeof(SavedPropertiesTypeCache).GetField(
            "_netIdToPropertyNameMap",
            BindingFlags.Static | BindingFlags.NonPublic);
        var propertyNames = (List<string>?)field?.GetValue(null)
                            ?? throw new InvalidOperationException(
                                "Failed to read SavedPropertiesTypeCache property name map.");

        var count = propertyNames.Count;
        var bitSize = count <= 1 ? 0 : (int)Math.Ceiling(Math.Log2(count));

        var property = typeof(SavedPropertiesTypeCache).GetProperty(
            nameof(SavedPropertiesTypeCache.NetIdBitSize),
            BindingFlags.Static | BindingFlags.Public);
        property?.SetValue(null, bitSize);

        return count;
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

internal sealed record SavedPropertyBootstrapResult(
    int DiscoveredTypeCount,
    int InjectedTypeCount,
    int DiscoveredPropertyCount,
    int TotalPropertyNameCount,
    int NetIdBitSize,
    string Fingerprint);
