using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive))]
public static class AbyssalTentacleDamageAdditivePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref decimal __result)
    {
        if (!AbyssalTentacleCombatHelper.IsIsolatedDamageActive)
            return true;

        __result = 0m;
        return false;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageMultiplicative))]
public static class AbyssalTentacleDamageMultiplicativePatch
{
    [HarmonyPrefix]
    public static bool Prefix(ref decimal __result)
    {
        if (!AbyssalTentacleCombatHelper.IsIsolatedDamageActive)
            return true;

        __result = 1m;
        return false;
    }
}
