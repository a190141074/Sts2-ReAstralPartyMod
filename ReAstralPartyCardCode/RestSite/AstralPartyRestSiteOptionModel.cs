using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public abstract partial class AstralPartyRestSiteOptionModel : ModRestSiteOptionTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();
    private static readonly string ModIdPrefix = ToUpperSnakeCase(MainFile.ModId);

    protected virtual string OptionName => GetType().Name.EndsWith("RestSiteOption", StringComparison.Ordinal)
        ? GetType().Name[..^"RestSiteOption".Length]
        : GetType().Name;

    protected virtual string OptionIdValue => $"{ModIdPrefix}_OPTION_{ToUpperSnakeCase(OptionName)}";

    protected virtual string IconFileStem => ToLowerSnakeCase(OptionName);

    protected virtual string IconBasePath =>
        $"res://ReAstralPartyMod/images/ui/rest_site/{IconFileStem}";

    protected virtual string DefaultIconPath => $"{IconBasePath}.png";

    protected AstralPartyRestSiteOptionModel(Player owner) : base(owner)
    {
    }

    public override string OptionId => OptionIdValue;

    public override RestSiteOptionAssetProfile AssetProfile => new(DefaultIconPath);

    public static string GetOptionId<TOption>() where TOption : AstralPartyRestSiteOptionModel
    {
        var optionType = typeof(TOption);
        var optionName = optionType.Name.EndsWith("RestSiteOption", StringComparison.Ordinal)
            ? optionType.Name[..^"RestSiteOption".Length]
            : optionType.Name;
        return $"{ModIdPrefix}_OPTION_{ToUpperSnakeCase(optionName)}";
    }

    protected LocString RestSiteUiLoc(string suffix)
    {
        return new LocString("rest_site_ui", $"OPTION_{OptionId}.{suffix}");
    }

    private static string ToUpperSnakeCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return CamelCaseRegex.Replace(value, "$1_$2").Replace('-', '_').ToUpperInvariant();
    }

    private static string ToLowerSnakeCase(string value)
    {
        return ToUpperSnakeCase(value).ToLowerInvariant();
    }

    [GeneratedRegex(@"([a-z0-9])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
