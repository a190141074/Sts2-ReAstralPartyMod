using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Content;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Events;

public abstract partial class AstralPartyEventModel : ModEventTemplate
{
    private static readonly Regex CamelCaseRegex = MyRegex();

    protected virtual string EventId => CamelCaseRegex.Replace(GetType().Name, "$1_$2").ToLowerInvariant();
    protected virtual string PortraitBasePath => $"res://ReAstralPartyMod/images/events/{EventId}";
    protected virtual string PortraitPath => $"{PortraitBasePath}.png";

    // EventModel will still try to preload the vanilla default portrait path for normal events.
    // Filter it out so our mod events only use the explicit AssetProfile portrait path.
    protected virtual string VanillaDefaultPreloadPortraitPath =>
        $"res://images/events/{Id.Entry.ToLowerInvariant()}.png";

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: PortraitPath);

    public override IEnumerable<string> GetAssetPaths(IRunState runState)
    {
        return base.GetAssetPaths(runState)
            .Where(path => !string.Equals(path, VanillaDefaultPreloadPortraitPath, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"([a-z])([A-Z])", RegexOptions.Compiled)]
    private static partial Regex MyRegex();
}
