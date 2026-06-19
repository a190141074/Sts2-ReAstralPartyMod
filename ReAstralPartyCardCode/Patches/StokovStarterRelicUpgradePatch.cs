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
        return [new(typeof(RelicCmd), nameof(RelicCmd.Replace), [typeof(RelicModel), typeof(RelicModel)])];
    }

    public sealed class ReplaceState
    {
        public Player? Owner { get; init; }
        public required ModelId OriginalRelicId { get; init; }
    }

    public static void Prefix(RelicModel original, out ReplaceState __state)
    {
        __state = new ReplaceState
        {
            Owner = original.Owner,
            OriginalRelicId = original.CanonicalInstance?.Id ?? original.Id
        };
    }

    public static void Postfix(ReplaceState __state, ref Task<RelicModel> __result)
    {
        __result = RunAfterReplaceAsync(__result, __state.Owner, __state.OriginalRelicId);
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
