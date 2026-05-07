using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Potions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(Player), "PopulateStartingInventory")]
// Starting potion grant disabled: persona chest remains registered for compatibility only.
public static class StartingPotionPatch
{
    [HarmonyPostfix]
    public static void Postfix(Player __instance)
    {
    }
}
