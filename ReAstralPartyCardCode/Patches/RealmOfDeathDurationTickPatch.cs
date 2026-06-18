using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Hooks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class RealmOfDeathDurationTickPatch : IPatchMethod
{
    public static string PatchId => "realm_of_death_duration_tick_patch";

    public static string Description =>
        "Gameplay patch: tick down all Realm of Death powers once at enemy-side turn end";

    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(Hook), nameof(Hook.AfterTurnEnd), [typeof(CombatState), typeof(CombatSide)])];
    }

    public static void Postfix(CombatState combatState, CombatSide side, ref Task __result)
    {
        __result = RunAfterTurnEnd(__result, combatState, side);
    }

    private static async Task RunAfterTurnEnd(Task originalTask, CombatState combatState, CombatSide side)
    {
        await originalTask;

        if (side != CombatSide.Enemy || combatState == null)
            return;

        var powers = combatState.Creatures
            .Select(creature => creature.GetPower<RealmOfDeathPower>())
            .Where(power => power != null)
            .ToList();

        foreach (var power in powers)
            await PowerCmd.TickDownDuration(power!);
    }
}
