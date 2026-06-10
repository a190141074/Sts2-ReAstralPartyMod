using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class MoonPropEulogyZeroObtainHelper
{
    private const string ContextId = "moon_prop_eulogy_zero_obtain";

    public static void TryReplaceObtainedRelic(ref RelicModel relic, Player? player)
    {
        if (player?.RunState == null)
            return;
        if (MoonPropShopExtraRelicsHelper.IsMoonPropRelic(relic))
            return;
        if (player.GetRelic<MoonPropEulogyZero>() is not { IsMelted: false } eulogyZero)
            return;
        if (!eulogyZero.RollShouldReplace(relic))
            return;

        var originalRelicId = (relic.CanonicalInstance ?? relic).Id;
        relic = MoonPropShopExtraRelicsHelper.CreateDeterministicMoonPropRelic(
            player,
            ContextId,
            originalRelicId.Entry,
            eulogyZero.GetStacks(),
            eulogyZero.AstralParty_MoonPropEulogyZeroRollCounter);
        eulogyZero.Flash();
    }
}

public sealed class MoonPropEulogyZeroObtainPatch : IPatchMethod
{
    public static string PatchId => "moon_prop_eulogy_zero_obtain";

    public static string Description => "Replace normal relic obtains with deterministic Moon Prop relics while Moon Prop Eulogy Zero is held";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)])];
    }

    public static void Prefix(ref RelicModel relic, Player player)
    {
        MoonPropEulogyZeroObtainHelper.TryReplaceObtainedRelic(ref relic, player);
    }
}
