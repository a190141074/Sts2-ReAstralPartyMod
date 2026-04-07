using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace AstralPartyMod.AstralPartyCardCode.Powers;

public abstract partial class AstralPartyPowerModel : CustomPowerModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PowerId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    public override string? CustomPackedIconPath => $"res://AstralPartyMod/images/powers/{PowerId}.png";

    public override string? CustomBigIconPath => $"res://AstralPartyMod/images/powers/{PowerId}.png";

    public static string GeneratePowerId<T>() where T : class
    {
        var typeName = typeof(T).Name;
        return CamelCaseRegex.Replace(typeName, "$1_$2").ToLowerInvariant();
    }

    public static string GenerateIconPath<T>() where T : class
    {
        return $"res://AstralPartyMod/images/powers/{GeneratePowerId<T>()}.png";
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
