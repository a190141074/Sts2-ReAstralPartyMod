using System.Linq;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class EnigmaticSynthesisXpScrollRestSitePatch : IPatchMethod
{
    public static string PatchId => "enigmatic_synthesis_xp_scroll_rest_site";

    public static string Description => "Track whether Enigmatic Synthesis XP Scroll owner skipped Smith at a rest site";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(NRestSiteRoom),
                "OnAfterPlayerSelectedRestSiteOption",
                [typeof(RestSiteOption), typeof(bool), typeof(ulong)])
        ];
    }

    public static void Postfix(NRestSiteRoom __instance, RestSiteOption option, bool success, ulong playerId)
    {
        if (!success)
            return;

        var player = __instance.Characters.FirstOrDefault(character => character.Player.NetId == playerId)?.Player;
        player?.GetRelic<EnigmaticSynthesisXpScroll>()?.OnRestSiteOptionResolved(option);
    }
}
