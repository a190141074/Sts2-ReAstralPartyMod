using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using Godot;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

public abstract partial class AstralPartyRelicModel : CustomRelicModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string IconBasePath => $"res://AstralPartyMod/images/relic/{RelicId}";

    public override string PackedIconPath => $"{IconBasePath}.png";

    protected override string BigIconPath => $"{IconBasePath}.png";

    protected override string PackedIconOutlinePath => $"{IconBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}