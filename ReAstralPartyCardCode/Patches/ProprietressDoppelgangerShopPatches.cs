using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Merchant;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class ProprietressDoppelgangerShopPatchHelper
{
    public static async Task<(bool, int)> HandlePostPurchaseAsync(Task<(bool, int)> originalTask,
        MerchantInventory? inventory)
    {
        var result = await originalTask;
        if (!result.Item1 || inventory?.Player == null || result.Item2 <= 0)
            return result;

        await ProprietressDoppelgangerShopHelper.TryProcessPurchaseAsync(inventory.Player, result.Item2);
        return result;
    }
}

public sealed class ProprietressDoppelgangerShopRelicPurchasePatch : IPatchMethod
{
    public static string PatchId => "proprietress_doppelganger_shop_relic_purchase";
    public static string Description => "Process Proprietress doppelganger bonuses after successful relic purchases";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(MerchantRelicEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])
        ];
    }

    public static Task<(bool, int)> Postfix(Task<(bool, int)> __result, MerchantInventory? inventory)
    {
        return ProprietressDoppelgangerShopPatchHelper.HandlePostPurchaseAsync(__result, inventory);
    }
}

public sealed class ProprietressDoppelgangerShopCardPurchasePatch : IPatchMethod
{
    public static string PatchId => "proprietress_doppelganger_shop_card_purchase";
    public static string Description => "Process Proprietress doppelganger bonuses after successful card purchases";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(MerchantCardEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])
        ];
    }

    public static Task<(bool, int)> Postfix(Task<(bool, int)> __result, MerchantInventory? inventory)
    {
        return ProprietressDoppelgangerShopPatchHelper.HandlePostPurchaseAsync(__result, inventory);
    }
}

public sealed class ProprietressDoppelgangerShopPotionPurchasePatch : IPatchMethod
{
    public static string PatchId => "proprietress_doppelganger_shop_potion_purchase";
    public static string Description => "Process Proprietress doppelganger bonuses after successful potion purchases";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(MerchantPotionEntry), "OnTryPurchase", [typeof(MerchantInventory), typeof(bool)])
        ];
    }

    public static Task<(bool, int)> Postfix(Task<(bool, int)> __result, MerchantInventory? inventory)
    {
        return ProprietressDoppelgangerShopPatchHelper.HandlePostPurchaseAsync(__result, inventory);
    }
}

public sealed class ProprietressDoppelgangerShopRemovalPurchasePatch : IPatchMethod
{
    public static string PatchId => "proprietress_doppelganger_shop_removal_purchase";

    public static string Description =>
        "Process Proprietress doppelganger bonuses after successful card removal purchases";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new ModPatchTarget(typeof(MerchantCardRemovalEntry), "OnTryPurchase",
                [typeof(MerchantInventory), typeof(bool)])
        ];
    }

    public static Task<(bool, int)> Postfix(Task<(bool, int)> __result, MerchantInventory? inventory)
    {
        return ProprietressDoppelgangerShopPatchHelper.HandlePostPurchaseAsync(__result, inventory);
    }
}
