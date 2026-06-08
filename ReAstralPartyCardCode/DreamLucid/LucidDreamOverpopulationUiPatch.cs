using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ReAstralPartyMod.ReAstralPartyCardCode.DreamLucid;

[HarmonyPatch(typeof(NCombatRoom), nameof(NCombatRoom.AddCreature))]
public static class LucidDreamOverpopulationUiPatch
{
    [HarmonyPostfix]
    public static void Postfix(NCombatRoom __instance, Creature creature)
    {
        if (!LucidDreamMaliceRuntimeHelper.ConsumeOverpopulationSpawnMark(creature))
            return;
        if (creature.SlotName != null)
            return;

        var creatureNode = __instance.GetCreatureNode(creature);
        if (creatureNode == null)
            return;

        var totalEnemies = __instance.CreatureNodes.Count(node => node.Entity.IsEnemy);
        var index = Math.Max(0, totalEnemies - 1);
        var x = 680f + index * 165f;
        var y = 200f - (index % 2 == 0 ? 0f : 36f);
        creatureNode.Position = new Vector2(x, y);
    }
}
