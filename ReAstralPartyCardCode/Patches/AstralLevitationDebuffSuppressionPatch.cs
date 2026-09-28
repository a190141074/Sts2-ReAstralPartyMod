using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class AstralLevitationDebuffSuppressionPatch
{
    public static void Postfix(AbstractModel __instance, ref bool __result)
    {
        if (!__result || __instance is not PowerModel power || power.Type != PowerType.Debuff)
            return;

        var owner = power.Owner;
        if (owner?.GetPower<AstralLevitationPower>() != null)
            __result = false;
    }
}
