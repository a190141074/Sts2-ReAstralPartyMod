using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class StokovStarterRelicUpgradePatch : IPatchMethod
{
    public static string PatchId => "stokov_starter_relic_upgrade_patch";

    public static string Description => "Propagate Touch of Orobas starter relic upgrades across the Stokov starter bundle";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RelicCmd), nameof(RelicCmd.Replace))];
    }

    public static void Prefix(RelicModel original, out Player? __stateOwner, out ModelId __stateOriginalRelicId)
    {
        __stateOwner = original.Owner;
        __stateOriginalRelicId = original.CanonicalInstance?.Id ?? original.Id;
    }

    public static void Postfix(Player? __stateOwner, ModelId __stateOriginalRelicId, ref Task<RelicModel> __result)
    {
        __result = RunAfterReplaceAsync(__result, __stateOwner, __stateOriginalRelicId);
    }

    private static async Task<RelicModel> RunAfterReplaceAsync(
        Task<RelicModel> originalTask,
        Player? owner,
        ModelId originalRelicId)
    {
        var replaced = await originalTask;
        if (owner != null)
            await StokovStarterBundleHelper.TryPropagateStarterRelicUpgradeAsync(owner, originalRelicId);
        return replaced;
    }
}
