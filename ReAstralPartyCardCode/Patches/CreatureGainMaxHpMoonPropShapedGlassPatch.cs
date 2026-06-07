using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureGainMaxHpMoonPropShapedGlassPatch : IPatchMethod
{
    public static string PatchId => "creature_gain_max_hp_moon_prop_shaped_glass";
    public static string Description => "Clamp current HP to 50% after max HP gain while Moon Prop Shaped Glass is active";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.GainMaxHp))];
    }

    public static void Postfix(Creature creature, ref Task __result)
    {
        __result = RunAfter(__result, creature);
    }

    private static async Task RunAfter(Task originalTask, Creature creature)
    {
        await originalTask;
        await MoonPropShapedGlassHelper.TryClampCurrentHpAsync(creature);
    }
}
