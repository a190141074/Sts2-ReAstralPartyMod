using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldReceiveCombatHooks), MethodType.Getter)]
internal static class TetraWarforgeEnchantmentShouldReceiveCombatHooksPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (TetraWarforgeEnchantmentHelper.ShouldForceCombatHooks(__instance))
            __result = true;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
internal static class TetraWarforgeEnchantmentAfterCardPlayedPatch
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
        PlayerChoiceContext context,
        CardPlay cardPlay)
    {
        await originalTask;
        if (cardPlay.Card != card)
            return;

        await TetraWarforgeEnchantmentHelper.HandleAfterCardPlayed(context, cardPlay);
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive))]
internal static class TetraWarforgeEnchantmentModifyDamagePatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (__instance is not CardModel card)
            return;

        __result += TetraWarforgeEnchantmentHelper.GetDamagePenalty(card, cardSource);
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyBlockAdditive))]
internal static class TetraWarforgeEnchantmentModifyBlockPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        Creature target,
        decimal block,
        ValueProp props,
        CardModel? cardSource,
        CardPlay? cardPlay,
        ref decimal __result)
    {
        if (__instance is not CardModel card)
            return;

        __result += TetraWarforgeEnchantmentHelper.GetBlockPenalty(card, cardSource);
    }
}

[HarmonyPatch(typeof(EnchantmentModel), nameof(EnchantmentModel.DynamicExtraCardText), MethodType.Getter)]
internal static class TetraWarforgeEnchantmentExtraCardTextPatch
{
    [HarmonyPostfix]
    public static void Postfix(EnchantmentModel __instance, ref LocString? __result)
    {
        var replacement = TetraWarforgeEnchantmentHelper.ResolveDynamicExtraCardText(__instance);
        if (replacement != null)
            __result = replacement;
    }
}
