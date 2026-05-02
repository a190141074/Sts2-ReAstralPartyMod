using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class InitialPointRestSiteOption : ModRestSiteOptionTemplate
{
    public const string OptionKey = "RE_ASTRAL_PARTY_MOD_OPTION_INITIAL_POINT";

    private const string IconPath =
        "res://ReAstralPartyMod/images/ui/rest_site/option_re_astral_party_mod_option_initial_point.png";

    private readonly TokenGoldInitialPoint _relic;

    public InitialPointRestSiteOption(Player owner, TokenGoldInitialPoint relic) : base(owner)
    {
        _relic = relic;
        IsEnabled = relic.IsRestSiteOptionEnabled();
    }

    public override string OptionId => OptionKey;

    public override RestSiteOptionAssetProfile AssetProfile => new(IconPath);

    public override LocString Description => _relic.BuildRestSiteDescription();

    public override Task<bool> OnSelect()
    {
        return _relic.ResolveRestSiteOptionSelection();
    }
}