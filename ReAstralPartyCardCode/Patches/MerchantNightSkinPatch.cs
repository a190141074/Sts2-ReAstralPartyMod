using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class MerchantNightSkinPatch : IPatchMethod
{
    public static string PatchId => "merchant_night_skin_patch";

    public static string Description => "Gameplay patch: inject Night Skin into merchant relic inventory when a Sara holder is in the party";

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(MerchantRoom), nameof(MerchantRoom.EnterInternal), [typeof(IRunState), typeof(bool)])];
    }

    public static void Postfix(MerchantRoom __instance)
    {
        var inventory = __instance.Inventory;
        var player = inventory?.Player;
        var runState = player?.RunState;
        if (inventory == null || player == null || runState == null)
            return;
        if (!ShouldInject(runState, inventory))
            return;

        var relic = ModelDb.Relic<JewelryNightSkin>().ToMutable();
        inventory.AddRelicEntry(new MerchantRelicEntry(relic, player));
        MainFile.Logger.Info($"[MerchantNightSkin] Injected Night Skin into merchant inventory | player={player.NetId} | relicEntries={inventory.RelicEntries.Count}");
    }

    private static bool ShouldInject(IRunState runState, MerchantInventory inventory)
    {
        if (!runState.Players.Any(player => player.GetRelic<VariantPersonSara>() != null))
            return false;
        if (runState.Players.Any(player => player.GetRelic<JewelryNightSkin>() != null))
            return false;
        if (inventory.RelicEntries.Any(entry => entry.Model?.Id == ModelDb.Relic<JewelryNightSkin>().Id))
            return false;

        return true;
    }
}
