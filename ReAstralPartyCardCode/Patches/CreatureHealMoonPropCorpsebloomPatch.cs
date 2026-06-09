using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class CreatureHealMoonPropCorpsebloomPatch : IPatchMethod
{
    public readonly record struct HealState(bool Active, bool IsInCombat, decimal AttemptedHeal, decimal HpBeforeHeal);

    public static string PatchId => "creature_heal_moon_prop_corpsebloom";
    public static string Description => "Boost healing for Moon Prop Corpsebloom owners and convert in-combat overflow to Half Life Heal";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(CreatureCmd), nameof(CreatureCmd.Heal))];
    }

    public static void Prefix(Creature creature, ref decimal amount, out HealState __state)
    {
        __state = default;
        if (creature?.Player?.GetRelic<MoonPropCorpsebloom>() is not { } relic || amount <= 0m)
            return;

        var attemptedHeal = relic.GetModifiedHealAmount(amount);
        var isInCombat = creature.CombatState != null;
        amount = attemptedHeal;
        if (isInCombat)
            amount = Math.Min(amount, relic.GetCombatHealCap(creature));

        __state = new HealState(true, isInCombat, attemptedHeal, creature.CurrentHp);
    }

    public static void Postfix(Creature creature, decimal amount, HealState __state, ref Task __result)
    {
        if (!__state.Active)
            return;

        __result = RunAfterAsync(__result, creature, amount, __state);
    }

    private static async Task RunAfterAsync(Task originalTask, Creature creature, decimal finalAmount, HealState state)
    {
        await originalTask;
        if (!state.IsInCombat || finalAmount <= 0m)
            return;

        var actualHealed = Math.Max(0m, creature.CurrentHp - state.HpBeforeHeal);
        var overflow = Math.Max(0m, state.AttemptedHeal - actualHealed);
        if (overflow <= 0m)
            return;

        await DerivedHealResolutionHelper.EnqueueHalfLifeHealAndFlush(
            creature,
            overflow,
            creature,
            null,
            "moon_prop_corpsebloom");
    }
}
