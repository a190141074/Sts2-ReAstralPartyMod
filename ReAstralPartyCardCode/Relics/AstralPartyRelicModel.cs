using System.Text.RegularExpressions;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

public abstract partial class AstralPartyRelicModel : ModRelicTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string RelicId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string IconBasePath => $"res://ReAstralPartyMod/images/relic/{RelicId}";
    protected new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override RelicAssetProfile AssetProfile => new()
    {
        IconPath = PackedIconPath,
        IconOutlinePath = PackedIconOutlinePath,
        BigIconPath = BigIconPath
    };

    public override string PackedIconPath => $"{IconBasePath}.png";
    public virtual string PublicBigIconPath => BigIconPath;
    protected override string BigIconPath => $"{IconBasePath}.png";
    protected override string PackedIconOutlinePath => $"{IconBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
