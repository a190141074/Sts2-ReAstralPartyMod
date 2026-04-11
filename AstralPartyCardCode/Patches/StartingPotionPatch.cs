using System.Linq;
using AstralPartyMod.AstralPartyCardCode.Potions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(typeof(Player), "PopulateStartingInventory")]
public static class StartingPotionPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player __instance)
    {
        var potionId = ModelDb.Potion<PersonChestChoose>().Id;
        if (__instance.Potions.Any(potion => potion.CanonicalInstance.Id == potionId))
            return;

        // Add the custom potion through the normal inventory path so every new player,
        // including multiplayer teammates, starts with the same potion setup.
        __instance.AddPotionInternal(ModelDb.Potion<PersonChestChoose>().ToMutable());
    }
}