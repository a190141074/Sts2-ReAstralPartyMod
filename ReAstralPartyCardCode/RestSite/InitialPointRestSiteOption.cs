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

namespace ReAstralPartyMod.ReAstralPartyCardCode.RestSite;

public class InitialPointRestSiteOption : AstralPartyRestSiteOptionModel
{
    public static string OptionKey => GetOptionId<InitialPointRestSiteOption>();

    public InitialPointRestSiteOption(Player owner) : base(owner)
    {
    }

    public override bool IsEnabled => ResolveRelic()?.IsRestSiteOptionEnabled(Owner) ?? false;

    public override IEnumerable<string> AssetPaths =>
        base.AssetPaths
            .Concat(NRestSmokeVfx.AssetPaths)
            .Concat(NDesaturateTransitionVfx.AssetPaths);

    public override LocString Description =>
        ResolveRelic()?.BuildRestSiteDescription(Owner)
        ?? RestSiteUiLoc("description_stage1");

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
        await Cmd.CustomScaledWait(1.5f, 2.5f, false, ct);
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
