using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(SovereignBlade), nameof(SovereignBlade.ModifyCardPlayResultPileTypeAndPosition))]
internal static class TyrantFormSovereignBladeResultPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        SovereignBlade __instance,
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position,
        ref (PileType, CardPilePosition) __result)
    {
        if (!ReferenceEquals(__instance, card))
            return;
        if (__instance.Owner?.Creature?.HasPower<TyrantFormPower>() != true)
            return;

        __result = (PileType.Hand, CardPilePosition.Top);
    }
}
