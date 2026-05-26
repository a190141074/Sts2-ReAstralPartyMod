using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class AstralStableRandom
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static int Index(IRunState runState, int count, params object?[] saltParts)
    {
        ArgumentNullException.ThrowIfNull(runState);
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), count, "Cannot choose from an empty pool.");

        return (int)(Hash(runState, saltParts) % (ulong)count);
    }

    public static bool PercentChance(IRunState runState, int percent, params object?[] saltParts)
    {
        if (percent <= 0)
            return false;
        if (percent >= 100)
            return true;

        return Index(runState, 100, saltParts) < percent;
    }

    public static T? Pick<T>(
        IEnumerable<T> candidates,
        Func<T, string> keySelector,
        IRunState runState,
        params object?[] saltParts)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);

        var pool = candidates
            .Select(item => (Item: item, Key: keySelector(item)))
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ToList();
        if (pool.Count == 0)
            return default;

        var poolKey = string.Join(",", pool.Select(static entry => entry.Key));
        var index = Index(runState, pool.Count, AppendSalt(saltParts, "pool", poolKey));
        return pool[index].Item;
    }

    public static List<T> PickDistinct<T>(
        IEnumerable<T> candidates,
        int count,
        Func<T, string> keySelector,
        IRunState runState,
        params object?[] saltParts)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(runState);

        if (count <= 0)
            return [];

        var pool = candidates
            .Select(item => (Item: item, Key: keySelector(item)))
            .OrderBy(static entry => entry.Key, StringComparer.Ordinal)
            .ToList();
        var selected = new List<T>(Math.Min(count, pool.Count));
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            var poolKey = string.Join(",", pool.Select(static entry => entry.Key));
            var index = Index(runState, pool.Count, AppendSalt(saltParts, "pick", i, "pool", poolKey));
            selected.Add(pool[index].Item);
            pool.RemoveAt(index);
        }

        return selected;
    }

    public static string PlayerKey(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        var runState = (RunState)player.RunState;
        return $"{runState.GetPlayerSlotIndex(player)}:{player.NetId}";
    }

    private static ulong Hash(IRunState runState, IEnumerable<object?> saltParts)
    {
        var hash = OffsetBasis;
        Add(ref hash, runState.Rng.StringSeed);
        Add(ref hash, "|act:");
        Add(ref hash, runState.CurrentActIndex);
        Add(ref hash, "|floor:");
        Add(ref hash, runState.TotalFloor);
        foreach (var part in saltParts)
        {
            Add(ref hash, "|");
            Add(ref hash, part);
        }

        return hash;
    }

    private static object?[] AppendSalt(object?[] saltParts, params object?[] extra)
    {
        var result = new object?[saltParts.Length + extra.Length];
        Array.Copy(saltParts, result, saltParts.Length);
        Array.Copy(extra, 0, result, saltParts.Length, extra.Length);
        return result;
    }

    private static void Add(ref ulong hash, object? value)
    {
        if (value == null)
            return;

        var text = value.ToString();
        if (string.IsNullOrEmpty(text))
            return;

        foreach (var ch in text)
        {
            hash ^= ch;
            hash *= Prime;
        }
    }
}
