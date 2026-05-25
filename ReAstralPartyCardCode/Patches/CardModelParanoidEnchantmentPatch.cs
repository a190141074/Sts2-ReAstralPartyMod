using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldReceiveCombatHooks), MethodType.Getter)]
internal static class CardModelParanoidShouldReceiveCombatHooksPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (ParanoidEnchantmentHelper.ShouldForceCombatHooks(__instance))
            __result = true;
    }
}

[HarmonyPatch(typeof(CardModel), "IsPlayable", MethodType.Getter)]
internal static class CardModelParanoidIsPlayablePatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (ParanoidEnchantmentHelper.ShouldBlockManualPlay(__instance))
            __result = false;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCurrentHpChanged))]
internal static class CardModelParanoidAfterCurrentHpChangedPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        Creature creature,
        decimal delta,
        ref Task __result)
    {
        if (__instance is not CardModel card)
            return;

        __result = ContinueAfterOriginal(__result, card, creature, delta);
    }

    private static async Task ContinueAfterOriginal(
        Task originalTask,
        CardModel card,
        Creature creature,
        decimal delta)
    {
        await originalTask;
        await ParanoidEnchantmentHelper.TryAutoPlayOnOwnerHpLoss(card, creature, delta);
    }
}
