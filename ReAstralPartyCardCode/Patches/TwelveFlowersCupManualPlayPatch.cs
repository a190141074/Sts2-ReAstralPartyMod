using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class TwelveFlowersCupManualPlayPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (!__result)
            return;
        if (__instance.Type != CardType.Attack)
            return;
        if (VigilCounterAutoPlayHelper.IsCurrentlyAutoPlaying(__instance))
            return;
        if (__instance.Owner?.GetRelic<VariantPersonTwelveFlowersCup>() == null)
            return;
        if (!VariantPersonTwelveFlowersCup.IsBlockedManualAttackCost(__instance))
            return;

        __result = false;
    }
}
