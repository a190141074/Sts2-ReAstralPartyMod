using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class ProphecySoulDevourAncientRuinsRestSiteOption : AstralPartyRestSiteOptionModel
{
    protected override string OptionName => "ProphecySoulDevourAncientRuins";
    public override RestSiteOptionAssetProfile AssetProfile => new("res://images/ui/restsite/smith.png");

    public ProphecySoulDevourAncientRuinsRestSiteOption(Player owner) : base(owner)
    {
        IsEnabled = true;
    }

    public override LocString Description => Owner.GetRelic<ProphecySoulDevour>()?.BuildAncientRuinsRestSiteDescription()
        ?? ProphecySoulDevourRegistry.AncientRuinsRestSiteDescription;

    public override async Task<bool> OnSelect()
    {
        var relic = Owner.GetRelic<ProphecySoulDevour>();
        if (relic == null)
            return false;

        return await relic.ResolveAncientRuinsSmithAsync();
    }
}
