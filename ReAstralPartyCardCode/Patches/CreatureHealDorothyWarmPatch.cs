using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealDorothyWarmPatch : IPatchMethod
{
    public static string PatchId => "creature_heal_dorothy_warm";
    public static string Description => "Grant Warm to Dorothy when actual healing resolves";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static void Postfix(Creature creature, decimal amount)
    {
        if (creature?.Player == null || amount <= 0m)
            return;

        _ = PersonDorothyHaze.TryGainWarmFromHeal(creature.Player, amount);
    }
}
