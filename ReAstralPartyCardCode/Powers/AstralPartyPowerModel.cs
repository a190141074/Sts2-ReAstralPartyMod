using System.Text.RegularExpressions;
using Godot;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

[RegisterPower(Inherit = true)]
public abstract partial class AstralPartyPowerModel : ModPowerTemplate
{
    private const string MissingPowerIconPath = "res://images/powers/missing_power.png";
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override PowerAssetProfile AssetProfile => new()
    {
        IconPath = ResolveIconPath(),
        BigIconPath = ResolveIconPath()
    };

    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateIconPath<T>() where T : class
    {
        return AstralPartyAssetPaths.PowerIcon(GeneratePowerId<T>());
    }

    protected virtual string ResolveIconPath()
    {
        foreach (var path in GetCandidateIconPaths())
            if (ResourceLoader.Exists(path))
                return path;

        return MissingPowerIconPath;
    }

    protected virtual IEnumerable<string> GetCandidateIconPaths()
    {
        var idEntry = NormalizePowerImageId(Id.Entry);
        yield return $"res://ReAstralPartyMod/images/powers/{idEntry}.png";
        yield return $"res://ReAstralPartyMod/images/power/{idEntry}.png";
        yield return $"res://ReAstralPartyMod/images/powers/{PowerId}.png";
        yield return $"res://ReAstralPartyMod/images/power/{PowerId}.png";
        yield return MissingPowerIconPath;
    }

    protected virtual string NormalizePowerImageId(string idEntry)
    {
        var prefixSeparator = idEntry.IndexOf('-');
        if (prefixSeparator >= 0 && prefixSeparator < idEntry.Length - 1)
            idEntry = idEntry[(prefixSeparator + 1)..];

        return idEntry.ToLowerInvariant();
    }

    protected static PowerAssetProfile Icons(string iconPath, string? bigIconPath = null)
    {
        return new(iconPath, bigIconPath ?? iconPath);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
