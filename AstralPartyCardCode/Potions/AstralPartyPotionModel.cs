using System.Text.RegularExpressions;
using BaseLib.Abstracts;

namespace AstralPartyMod.AstralPartyCardCode.Potions;

public abstract partial class AstralPartyPotionModel : CustomPotionModel
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string PotionId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string ImageBasePath => $"res://AstralPartyMod/images/potion/{PotionId}";

    protected AstralPartyPotionModel(bool autoAdd = true) : base(autoAdd)
    {
    }

    public override string? CustomPackedImagePath => $"{ImageBasePath}.png";

    public override string? CustomPackedOutlinePath => $"{ImageBasePath}.png";

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}