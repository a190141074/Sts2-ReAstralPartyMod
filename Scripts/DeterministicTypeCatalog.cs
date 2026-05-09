using System.Reflection;

namespace ReAstralPartyMod;

internal static class DeterministicTypeCatalog
{
    private static readonly Dictionary<Assembly, IReadOnlyList<Type>> CachedSortedTypes = new();

    public static IReadOnlyList<Type> GetSortedTypes(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (CachedSortedTypes.TryGetValue(assembly, out var cachedTypes))
            return cachedTypes;

        var sortedTypes = assembly
            .GetTypes()
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        CachedSortedTypes[assembly] = sortedTypes;
        return sortedTypes;
    }

    public static IReadOnlyList<Type> GetAssignableTypes<TBase>(
        Assembly assembly,
        Func<Type, bool>? extraPredicate = null)
    {
        return GetSortedTypes(assembly)
            .Where(type =>
                type is { IsAbstract: false, IsClass: true }
                && typeof(TBase).IsAssignableFrom(type)
                && (extraPredicate?.Invoke(type) ?? true))
            .ToArray();
    }
}
