using System.Text.RegularExpressions;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Potions;

public abstract partial class AstralPartyPotionModel : ModPotionTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PotionId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string ImageBasePath => $"res://ReAstralPartyMod/images/potion/{PotionId}";
    public new virtual IEnumerable<IHoverTip> ExtraHoverTips => [];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => ExtraHoverTips;

    public override PotionAssetProfile AssetProfile => new()
    {
        ImagePath = $"{ImageBasePath}.png",
        OutlinePath = $"{ImageBasePath}.png"
    };

    protected AstralPartyPotionModel(bool autoAdd = true)
    {
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
