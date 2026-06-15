using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.RestSite;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class AstralPartyRestSiteOptionSuccessUiCompatPatch : IPatchMethod
{
    public static string PatchId => "astral_party_rest_site_option_success_ui_compat_patch";

    public static string Description =>
        "Compatibility patch: swallow third-party rest-site selected-option UI null references for successful ReAstralParty rest-site options";

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

    public static Exception? Finalizer(Exception? __exception, RestSiteOption option, bool success, ulong playerId)
    {
        if (__exception == null)
            return null;

        if (!success || option is not AstralPartyRestSiteOptionModel)
            return __exception;

        if (__exception is not NullReferenceException)
            return __exception;

        var stackTrace = __exception.StackTrace ?? string.Empty;
        if (!stackTrace.Contains("NRestSiteCharacter.ShowSelectedRestSiteOption", StringComparison.Ordinal))
            return __exception;

        MainFile.Logger.Warn(
            $"[AstralPartyRestSiteOptionSuccessUiCompatPatch] Swallowed third-party rest-site selected-option UI null reference for successful AstralParty rest-site option | player={playerId} | option={option.OptionId}");
        return null;
    }
}
