using System.Text.RegularExpressions;
using STS2RitsuLib.Scaffolding.Content;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

public abstract partial class AstralPartyEventModel : ModEventTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string EventId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();

    protected virtual string PortraitPath => AstralPartyAssetPaths.EventPortrait(EventId);

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath);

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
