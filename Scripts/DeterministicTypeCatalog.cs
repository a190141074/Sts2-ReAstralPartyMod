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

        var loadableTypes = GetLoadableTypes(assembly);
        var sortedTypes = loadableTypes
            .OrderBy(static type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        CachedSortedTypes[assembly] = sortedTypes;
        return sortedTypes;
    }

    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var loadableTypes = exception.Types
                .Where(static type => type != null)
                .Cast<Type>()
                .ToArray();

            foreach (var loaderException in exception.LoaderExceptions.OfType<Exception>())
                MainFile.Logger.Warn(
                    $"[DeterministicTypeCatalog] Reflection type load skipped for assembly '{assembly.GetName().Name}': {loaderException.GetType().Name}: {loaderException.Message}");

            if (loadableTypes.Length > 0)
                return loadableTypes;

            throw new InvalidOperationException(
                $"Failed to load any types from assembly '{assembly.FullName}' while building the deterministic type catalog.",
                exception);
        }
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
