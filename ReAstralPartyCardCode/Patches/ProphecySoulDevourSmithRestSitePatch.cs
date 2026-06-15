using HarmonyLib;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class ProphecySoulDevourSmithRestSitePatch : IPatchMethod
{
    public static string PatchId => "prophecy_soul_devour_smith_rest_site";

    public static string Description => "Intercept Smith rest-site resolution for Prophecy Soul Devour Ancient Ruins";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(SmithRestSiteOption),
                nameof(SmithRestSiteOption.OnSelect),
                [])
        ];
    }

    public static bool Prefix(SmithRestSiteOption __instance, ref Task<bool> __result)
    {
        var owner = Traverse.Create(__instance).Property("Owner").GetValue<Player>();
        if (owner == null)
            return true;

        var relic = owner.GetRelic<ProphecySoulDevour>();
        if (relic == null || !relic.ShouldInterceptSmithRestSiteOption())
            return true;

        __result = relic.ResolveAncientRuinsSmithAsync();
        return false;
    }
}
