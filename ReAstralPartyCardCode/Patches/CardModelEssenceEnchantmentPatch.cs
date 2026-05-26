using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.ShouldReceiveCombatHooks), MethodType.Getter)]
internal static class CardModelEssenceEnchantmentShouldReceiveCombatHooksPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (EyeOfSunEnchantmentHelper.ShouldForceCombatHooks(__instance)
            || SacredFaithEnchantmentHelper.ShouldForceCombatHooks(__instance))
            __result = true;
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterCardPlayed))]
internal static class CardModelEssenceEnchantmentAfterCardPlayedPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        PlayerChoiceContext context,
        CardPlay cardPlay,
        ref Task __result)
    {
        if (__instance is not CardModel card)
            return;

        __result = ContinueAfterOriginal(__result, card, context, cardPlay);
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

        await EyeOfSunEnchantmentHelper.HandleAfterCardPlayed(context, cardPlay);
        await UltimateSkillChargeHelper.HandleAfterCardPlayed(cardPlay);
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.ModifyDamageAdditive))]
internal static class CardModelEssenceEnchantmentModifyDamagePatch
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

        if (cardSource != card)
            return;

        __result += SacredFaithEnchantmentHelper.GetDamageBonus(card, amount);
    }
}

[HarmonyPatch(typeof(AbstractModel), nameof(AbstractModel.AfterDamageGiven))]
internal static class CardModelEssenceEnchantmentAfterDamageGivenPatch
{
    [HarmonyPostfix]
    public static void Postfix(
        AbstractModel __instance,
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource,
        ref Task __result)
    {
        if (__instance is not CardModel card)
            return;

        __result = ContinueAfterOriginal(__result, card, result, target, cardSource);
    }

    private static async Task ContinueAfterOriginal(
        Task originalTask,
        CardModel card,
        DamageResult result,
        Creature target,
        CardModel? cardSource)
    {
        await originalTask;
        if (cardSource != card)
            return;
        if (!result.WasTargetKilled)
            return;

        SacredFaithEnchantmentHelper.RegisterKill(card, target);
    }
}
