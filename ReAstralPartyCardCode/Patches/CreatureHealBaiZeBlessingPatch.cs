using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealBaiZeBlessingPatch : IPatchMethod
{
    public static string PatchId => "creature_heal_bai_ze_blessing";
    public static string Description => "Convert full-health healing into HalfLifeHeal while BaiZeBlessing is active";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static bool Prefix(Creature creature, ref decimal amount, out decimal __state)
    {
        __state = 0m;
        if (!BaiZeBlessingPower.ShouldConvertHeal(creature, amount))
            return true;

        __state = amount;
        amount = 0m;
        return true;
    }

    public static void Postfix(Creature creature, decimal __state, ref Task __result)
    {
        if (__state <= 0m)
            return;

        __result = RunAfterHeal(__result, creature, __state);
    }

    private static async Task RunAfterHeal(Task originalTask, Creature creature, decimal amount)
    {
        await originalTask;
        await PowerCmd.Apply<HalfLifeHealPower>(creature, amount, creature, null, false);
    }
}
