using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class UniversalAmplificationDisplayApplyPowerPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_apply_power_patch";
    public static string Description => "Refresh universal amplification display powers after powers are applied";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(PowerCmd),
                nameof(PowerCmd.Apply),
                [typeof(PowerModel), typeof(Creature), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])
        ];
    }

    public static void Postfix(Creature target, ref Task __result)
    {
        var player = target.Player;
        if (player == null)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplaySetPowerAmountPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_set_power_amount_patch";
    public static string Description => "Refresh universal amplification display powers after power amounts are modified";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(
                typeof(PowerCmd),
                nameof(PowerCmd.ModifyAmount),
                [typeof(PowerModel), typeof(decimal), typeof(Creature), typeof(CardModel), typeof(bool)])
        ];
    }

    public static void Postfix(PowerModel power, ref Task<int> __result)
    {
        var player = power.Owner?.Player;
        if (player == null)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task<int> ContinueAfterOriginal(Task<int> originalTask, Player player)
    {
        var result = await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
        return result;
    }
}

public sealed class UniversalAmplificationDisplayRemovePowerPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_remove_power_patch";
    public static string Description => "Refresh universal amplification display powers after powers are removed";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(PowerCmd), nameof(PowerCmd.Remove), [typeof(PowerModel)])];
    }

    public static void Postfix(PowerModel power, ref Task __result)
    {
        var player = power.Owner?.Player;
        if (player == null)
            return;
        if (power is AttackAmplificationDisplayPower or SkillAmplificationDisplayPower)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplayObtainRelicPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_obtain_relic_patch";
    public static string Description => "Refresh universal amplification display powers after relic obtains";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RelicCmd), nameof(RelicCmd.Obtain), [typeof(RelicModel), typeof(Player), typeof(int)])];
    }

    public static void Postfix(Player player, ref Task __result)
    {
        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplayRemoveRelicPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_remove_relic_patch";
    public static string Description => "Refresh universal amplification display powers after relic removals";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(RelicCmd), nameof(RelicCmd.Remove), [typeof(RelicModel)])];
    }

    public static void Postfix(RelicModel relic, ref Task __result)
    {
        var player = relic.Owner;
        if (player == null)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplayHealPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_heal_patch";
    public static string Description => "Refresh universal amplification display powers after healing changes stable amplification totals";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static void Postfix(Creature creature, ref Task __result)
    {
        var player = creature.Player;
        if (player == null)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplaySetCurrentHpPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_set_current_hp_patch";
    public static string Description => "Refresh universal amplification display powers after HP changes affect stable relic amplification";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.SetCurrentHp))];
    }

    public static void Postfix(Creature creature, ref Task __result)
    {
        var player = creature.Player;
        if (player == null)
            return;

        __result = ContinueAfterOriginal(__result, player);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, Player player)
    {
        await originalTask;
        await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}

public sealed class UniversalAmplificationDisplayAfterTurnEndPatch : IPatchMethod
{
    public static string PatchId => "universal_amplification_display_after_turn_end_patch";
    public static string Description => "Refresh universal amplification display powers after side turn end";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(Hook), nameof(Hook.AfterTurnEnd),
                [typeof(ICombatState), typeof(CombatSide), typeof(IEnumerable<Creature>)])
        ];
    }

    public static void Postfix(ICombatState combatState, CombatSide side, ref Task __result)
    {
        __result = ContinueAfterOriginal(__result, combatState, side);
    }

    private static async Task ContinueAfterOriginal(Task originalTask, ICombatState combatState, CombatSide side)
    {
        await originalTask;
        if (combatState?.RunState?.Players == null)
            return;

        foreach (var player in combatState.RunState.Players.Where(player => player.Creature?.Side == side))
            await UniversalAmplificationDisplayHelper.RefreshAmplificationDisplayPowers(player);
    }
}
