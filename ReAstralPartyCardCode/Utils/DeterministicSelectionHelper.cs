using System.Security.Cryptography;
using System.Text;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class DeterministicSelectionHelper
{
    public static IReadOnlyList<int> PickDistinctIndices(int count, int upperExclusive, params object[] contextParts)
    {
        if (count <= 0 || upperExclusive <= 0)
            return [];

        var result = new List<int>(count);
        var seen = new HashSet<int>();
        for (var i = 0; i < upperExclusive && result.Count < count; i++)
        {
            var candidate = ComputeDeterministicIndex(upperExclusive, contextParts.Append(i).ToArray());
            if (seen.Add(candidate))
                result.Add(candidate);
        }

        if (result.Count >= count)
            return result;

        for (var fallback = 0; fallback < upperExclusive && result.Count < count; fallback++)
            if (seen.Add(fallback))
                result.Add(fallback);

        return result;
    }

    private static int ComputeDeterministicIndex(int upperExclusive, params object[] contextParts)
    {
        var bytes = Encoding.UTF8.GetBytes(string.Join("|",
            contextParts.Select(part => part?.ToString() ?? string.Empty)));
        var hash = SHA256.HashData(bytes);
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % upperExclusive);
    }
}
