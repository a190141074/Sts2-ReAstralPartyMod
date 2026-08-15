using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive))]
public static class AbyssalTentacleDamageAdditivePatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (!AbyssalTentacleCombatHelper.IsIsolatedDamage(target, amount, dealer, cardSource))
            return true;

        __result = 0m;
        return false;
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        Exception? __exception,
        ref decimal __result)
    {
        if (AbyssalTentacleCombatHelper.IsIsolatedDamage(target, amount, dealer, cardSource))
            __result = 0m;

        return __exception;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageMultiplicative))]
public static class AbyssalTentacleDamageMultiplicativePatch
{
    [HarmonyPrefix]
    public static bool Prefix(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (!AbyssalTentacleCombatHelper.IsIsolatedDamage(target, amount, dealer, cardSource))
            return true;

        __result = 1m;
        return false;
    }

    [HarmonyFinalizer]
    public static Exception? Finalizer(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        Exception? __exception,
        ref decimal __result)
    {
        if (AbyssalTentacleCombatHelper.IsIsolatedDamage(target, amount, dealer, cardSource))
            __result = 1m;

        return __exception;
    }
}
