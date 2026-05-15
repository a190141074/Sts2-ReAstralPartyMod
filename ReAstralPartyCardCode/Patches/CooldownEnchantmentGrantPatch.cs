using System;
using System.Reflection;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch]
public static class CooldownEnchantmentGrantPatch
{
    public static MethodBase? TargetMethod()
    {
        return AccessTools.DeclaredMethod(
                   typeof(CardPileCmd),
                   nameof(CardPileCmd.AddGeneratedCardToCombat),
                   [typeof(CardModel), typeof(PileType), typeof(bool), typeof(CardPilePosition)]
               )
               ?? AccessTools.DeclaredMethod(
                   typeof(CardPileCmd),
                   nameof(CardPileCmd.AddGeneratedCardToCombat),
                   [typeof(CardModel), typeof(PileType), typeof(bool)]
               );
    }

    [HarmonyPrefix]
    public static void Prefix(CardModel card)
    {
        if (!AstralPartyCardModel.ShouldAutoApplyCooldown(card))
            return;

        if (card is not AstralPartyCardModel astralCard)
            return;

        astralCard.EnsureCooldownEnchantment().GetAwaiter().GetResult();
    }
}
