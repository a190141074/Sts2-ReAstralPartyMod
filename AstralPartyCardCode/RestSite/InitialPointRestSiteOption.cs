using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace AstralPartyMod.AstralPartyCardCode.RestSite;

public class InitialPointRestSiteOption : DigRestSiteOption
{
    public const string OptionKey = "OPTION_INITIAL_POINT";

    private static readonly string[] SharedAssetPaths = ["res://images/ui/rest_site/option_dig.png"];

    private readonly TokenGoldInitialPoint _relic;

    public InitialPointRestSiteOption(Player owner, TokenGoldInitialPoint relic) : base(owner)
    {
        _relic = relic;
        IsEnabled = relic.IsRestSiteOptionEnabled();
    }

    public override string OptionId => OptionKey;

    public override IEnumerable<string> AssetPaths => SharedAssetPaths;

    public override LocString Description => _relic.BuildRestSiteDescription();

    public override Task<bool> OnSelect()
    {
        return _relic.ResolveRestSiteOptionSelection();
    }
}
