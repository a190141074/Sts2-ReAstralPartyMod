using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class InitialPointRestSiteOption : ModRestSiteOptionTemplate
{
    public const string OptionKey = "RE_ASTRAL_PARTY_MOD_OPTION_INITIAL_POINT";

    private const string IconPath =
        "res://ReAstralPartyMod/images/ui/rest_site/option_re_astral_party_mod_option_initial_point.png";

    public InitialPointRestSiteOption(Player owner) : base(owner)
    {
        IsEnabled = ResolveRelic()?.IsRestSiteOptionEnabled(owner) ?? false;
    }

    public override string OptionId => OptionKey;

    public override IEnumerable<string> AssetPaths =>
        base.AssetPaths
            .Concat(NRestSmokeVfx.AssetPaths)
            .Concat(NDesaturateTransitionVfx.AssetPaths);

    public override RestSiteOptionAssetProfile AssetProfile => new(IconPath);

    public override LocString Description =>
        ResolveRelic()?.BuildRestSiteDescription(Owner)
        ?? new LocString("rest_site_ui", $"OPTION_{OptionKey}.description_stage1");

    public override Task<bool> OnSelect()
    {
        var relic = ResolveRelic();
        return relic?.ResolveRestSiteOptionSelection(Owner) ?? Task.FromResult(false);
    }

    public override async Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        HealRestSiteOption.PlayRestSiteHealSfx();
        NRestSiteRoom.Instance?.AddChildSafely(NRestSmokeVfx.Create());
        NRestSiteRoom.Instance?.AddChildSafely(NDesaturateTransitionVfx.Create());
        await Cmd.CustomScaledWait(1.5f, 2.5f, ignoreCombatEnd: false, ct);
    }

    public override Task DoRemotePostSelectVfx()
    {
        var character = NRestSiteRoom.Instance?.Characters.FirstOrDefault(candidate => candidate.Player == Owner);
        if (character == null)
            return Task.CompletedTask;

        character.Shake();
        return Task.CompletedTask;
    }

    private TokenGoldInitialPoint? ResolveRelic()
    {
        return Owner.GetRelic<TokenGoldInitialPoint>();
    }
}
