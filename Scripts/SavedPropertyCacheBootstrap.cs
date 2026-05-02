using System.Reflection;
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

        var modelTypes = assembly
            .GetTypes()
            .Where(static type =>
                !type.IsAbstract &&
                typeof(AbstractModel).IsAssignableFrom(type) &&
                HasSavedProperties(type))
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

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
            SavedPropertiesTypeCache.NetIdBitSize);
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
}

internal sealed record SavedPropertyBootstrapResult(
    int DiscoveredTypeCount,
    int InjectedTypeCount,
    int DiscoveredPropertyCount,
    int TotalPropertyNameCount,
    int NetIdBitSize);
