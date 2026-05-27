using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class TyrantFormManualPlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result)
            return;
        if (__instance is not SovereignBlade sovereignBlade)
            return;
        if (sovereignBlade.Owner?.Creature?.HasPower<TyrantFormPower>() != true)
            return;
        if (TyrantFormAutoPlayHelper.IsCurrentlyAutoPlaying(sovereignBlade))
            return;

        __result = false;
    }
}
