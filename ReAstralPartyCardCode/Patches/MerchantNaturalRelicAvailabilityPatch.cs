using System;
using System.Collections.Generic;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class MerchantNaturalRelicAvailabilityHelper
{
    public static void ReplaceDisallowedNaturalEntries(MerchantInventory? inventory, Player? player)
    {
        if (inventory == null || player?.RunState == null)
            return;
        if (!TryGetMutableRelicEntries(inventory, out var relicEntries))
            return;

        var replacedCount = 0;
        for (var index = 0; index < relicEntries.Count; index++)
        {
            var entry = relicEntries[index];
            if (AstralRelicAvailabilityHelper.IsRelicEnabledForRun(player.RunState, entry.Model))
                continue;

            var replacement = TryCreateAllowedMerchantRelic(player);
            if (replacement == null)
            {
                MainFile.Logger.Warn(
                    $"[{MainFile.ModId}] Failed to replace disallowed merchant relic entry | player={player.NetId} floor={player.RunState.TotalFloor} relic={entry.Model?.Id}.");
                continue;
            }

            relicEntries[index] = new MerchantRelicEntry(replacement, player);
            replacedCount++;
        }

        if (replacedCount > 0)
            MainFile.Logger.Info(
                $"[{MainFile.ModId}] Replaced {replacedCount} disallowed natural merchant relic entries | player={player.NetId} floor={player.RunState.TotalFloor}.");
    }

    public static bool IsFakeMerchantInventory(NMerchantInventory merchantInventory)
    {
        return string.Equals(merchantInventory.GetType().Name, "NFakeMerchantInventory", StringComparison.Ordinal);
    }

    private static bool TryGetMutableRelicEntries(
        MerchantInventory inventory,
        out IList<MerchantRelicEntry> relicEntries)
    {
        if (inventory.RelicEntries is IList<MerchantRelicEntry> directList)
        {
            relicEntries = directList;
            return true;
        }

        var backingField = typeof(MerchantInventory).GetField(
            "<RelicEntries>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (backingField?.GetValue(inventory) is IList<MerchantRelicEntry> backingList)
        {
            relicEntries = backingList;
            return true;
        }

        relicEntries = null!;
        return false;
    }

    private static RelicModel? TryCreateAllowedMerchantRelic(Player player)
    {
        return TryPullAllowedRelic(player, RelicRarity.Shop)
               ?? TryPullAllowedRelic(player, RelicRarity.Uncommon)
               ?? TryPullAllowedRelic(player, RelicRarity.Rare)
               ?? TryPullAllowedRelic(player, RelicRarity.Common);
    }

    private static RelicModel? TryPullAllowedRelic(Player player, RelicRarity rarity)
    {
        var runState = player.RunState;
        if (runState == null)
            return null;

        var relic = player.RelicGrabBag.PullFromFront(
            rarity,
            candidate => AstralRelicAvailabilityHelper.IsRelicEnabledForRun(runState, candidate),
            runState);
        if (relic == null)
            return null;

        runState.SharedRelicGrabBag.Remove(relic);
        return relic.ToMutable();
    }
}

public sealed class MerchantNaturalRelicCreateInventoryPatch : IPatchMethod
{
    public static string PatchId => "merchant_natural_relic_create_inventory";

    public static string Description =>
        "Replace disallowed natural merchant relic entries after the normal merchant inventory is created";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(MerchantInventory), nameof(MerchantInventory.CreateForNormalMerchant),
                [typeof(Player)])
        ];
    }

    public static void Postfix(Player player, MerchantInventory __result)
    {
        MerchantNaturalRelicAvailabilityHelper.ReplaceDisallowedNaturalEntries(__result, player);
    }
}

public sealed class MerchantNaturalRelicInitializeInventoryPatch : IPatchMethod
{
    public static string PatchId => "merchant_natural_relic_initialize_inventory";

    public static string Description =>
        "Replace disallowed natural merchant relic entries before the merchant UI initializes";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(NMerchantInventory), nameof(NMerchantInventory.Initialize),
                [typeof(MerchantInventory), typeof(MerchantDialogueSet)])
        ];
    }

    public static void Prefix(NMerchantInventory __instance, MerchantInventory inventory)
    {
        if (MerchantNaturalRelicAvailabilityHelper.IsFakeMerchantInventory(__instance))
            return;

        MerchantNaturalRelicAvailabilityHelper.ReplaceDisallowedNaturalEntries(inventory, inventory.Player);
    }
}
