using System;
using System.Collections;
using System.Reflection;
using MegaCrit.Sts2.Core.Runs;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Online;

public static class MerchantUiRefreshHelper
{
    public static void TryRefreshCurrentMerchantUi(IRunState? runState)
    {
        if (runState?.CurrentRoom == null)
            return;

        try
        {
            var roomType = runState.CurrentRoom.GetType();
            var inventoryProperty = roomType.GetProperty("MerchantInventory",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var inventory = inventoryProperty?.GetValue(runState.CurrentRoom);
            if (inventory == null)
                return;

            TryInvokeInventoryRefresh(inventory);
            TryRefreshEntries(inventory);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Merchant UI refresh skipped after MembershipCard grant: {ex.Message}");
        }
    }

    private static void TryInvokeInventoryRefresh(object inventory)
    {
        var inventoryType = inventory.GetType();
        foreach (var methodName in new[] { "Refresh", "RefreshEntries", "UpdateAllEntries", "OnInventoryUpdated" })
        {
            var method = inventoryType.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                Type.DefaultBinder,
                Type.EmptyTypes,
                null);
            if (method == null)
                continue;

            method.Invoke(inventory, null);
            return;
        }
    }

    private static void TryRefreshEntries(object inventory)
    {
        var entriesProperty = inventory.GetType().GetProperty("AllEntries",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (entriesProperty?.GetValue(inventory) is not IEnumerable entries)
            return;

        foreach (var entry in entries)
        {
            if (entry == null)
                continue;

            var entryType = entry.GetType();
            foreach (var methodName in new[] { "OnMerchantInventoryUpdated", "Refresh", "UpdatePrice", "RefreshPrice" })
            {
                var method = entryType.GetMethod(methodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    Type.DefaultBinder,
                    Type.EmptyTypes,
                    null);
                if (method == null)
                    continue;

                method.Invoke(entry, null);
                break;
            }
        }
    }
}
