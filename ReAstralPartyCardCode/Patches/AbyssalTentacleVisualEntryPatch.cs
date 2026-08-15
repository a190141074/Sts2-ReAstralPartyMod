using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class AbyssalTentacleVisualEntryPatch
{
    [HarmonyPostfix]
    public static void Postfix(Creature __0)
    {
        if (__0.Monster is AbyssalTentacleMonster)
            AbyssalTentacleCombatHelper.PositionTentacle(__0);
    }
}
