using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldReceiveCombatHooks), MethodType.Getter)]
internal static class MiniLuluEnchantmentShouldReceiveCombatHooksPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (MiniLuluEnchantmentHelper.ShouldForceCombatHooks(__instance))
            __result = true;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
internal static class MiniLuluEnchantmentAfterCardPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ref Task __result)
    {
        if (__instance is not CardModel card)
            return;

        __result = ContinueAfterOriginal(__result, card, choiceContext, cardPlay);
    }

    private static async Task ContinueAfterOriginal(
        Task originalTask,
        CardModel card,
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay)
    {
        await originalTask;
        if (cardPlay.Card != card)
            return;

        await MiniLuluEnchantmentHelper.HandleAfterCardPlayed(choiceContext, cardPlay);
    }
}
