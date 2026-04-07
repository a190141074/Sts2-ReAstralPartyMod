using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public abstract partial class AstralPartyPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    public override string? CustomPackedIconPath => ResolveIconPath();

    public override string? CustomBigIconPath => ResolveIconPath();

    public override string? CustomBigBetaIconPath => ResolveIconPath();

    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateIconPath<T>() where T : class
    {
        return $"res://AstralPartyMod/images/powers/{GeneratePowerId<T>()}.png";
    }

    protected virtual string ResolveIconPath()
    {
        foreach (string path in GetCandidateIconPaths())
        {
            if (ResourceLoader.Exists(path)) return path;
        }

        return "res://AstralPartyMod/images/powers/power.png";
    }

    protected virtual IEnumerable<string> GetCandidateIconPaths()
    {
        string idEntry = NormalizePowerImageId(Id.Entry);
        yield return $"res://AstralPartyMod/images/powers/{idEntry}.png";
        yield return $"res://AstralPartyMod/images/power/{idEntry}.png";
        yield return $"res://AstralPartyMod/images/powers/{PowerId}.png";
        yield return $"res://AstralPartyMod/images/power/{PowerId}.png";
        yield return "res://AstralPartyMod/images/powers/power.png";
    }

    protected virtual string NormalizePowerImageId(string idEntry)
    {
        int prefixSeparator = idEntry.IndexOf('-');
        if (prefixSeparator >= 0 && prefixSeparator < idEntry.Length - 1)
        {
            idEntry = idEntry[(prefixSeparator + 1)..];
        }

        return idEntry.ToLowerInvariant();
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
