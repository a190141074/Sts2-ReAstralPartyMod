using AstralPartyMod.AstralPartyCardCode.cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.AddGeneratedCardToCombat),
    [typeof(CardModel), typeof(PileType), typeof(bool)])]
public static class CooldownEnchantmentGrantPatch
{
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
