using System.Globalization;
using System.Text.Json;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class StableNumericStateHelper
{
    private static readonly NumberStyles DecimalNumberStyles = NumberStyles.Number;

    // Keep fractional math during calculation, but normalize at save/apply boundaries so persistence and sync only see stable scalar shapes.
    public static string SerializeDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static decimal DeserializeDecimal(string? value, decimal fallback = 0m)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return decimal.TryParse(value, DecimalNumberStyles, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    public static string SerializeDecimalSequence(IReadOnlyList<decimal> values)
    {
        return JsonSerializer.Serialize(values.Select(SerializeDecimal).ToArray());
    }

    public static List<decimal> DeserializeDecimalSequence(string? value)
    {
        try
        {
            var raw = JsonSerializer.Deserialize<string[]>(value ?? string.Empty) ?? [];
            return raw
                .Select(item => DeserializeDecimal(item, -1m))
                .Where(item => item >= 0m)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static int FloorToNonNegativeInt(decimal value)
    {
        return Math.Max(0, (int)decimal.Floor(value));
    }

    public static int RoundToNonNegativeInt(decimal value)
    {
        return Math.Max(0, Convert.ToInt32(decimal.Round(value, 0, MidpointRounding.AwayFromZero)));
    }

    public static int FloorDivisionToNonNegativeInt(decimal dividend, decimal divisor)
    {
        if (divisor <= 0m)
            return 0;

        return FloorToNonNegativeInt(dividend / divisor);
    }

    public static int ClampCeilingToInt(decimal value, decimal minInclusive, decimal maxInclusive)
    {
        return (int)Math.Clamp(Math.Ceiling(value), minInclusive, maxInclusive);
    }
}
