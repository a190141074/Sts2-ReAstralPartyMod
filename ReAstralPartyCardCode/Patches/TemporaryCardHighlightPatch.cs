using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.UpdateCard))]
public static class TemporaryCardHighlightPatch
{
    private static readonly Color TemporaryHighlightColor = new(0.58f, 0.84f, 1f, 1f);

    [HarmonyPostfix]
    public static void UpdateCardPostfix(NHandCardHolder __instance)
    {
        ApplyTemporaryHighlight(__instance);
    }

    [HarmonyPatch(typeof(NHandCardHolder), nameof(NHandCardHolder.Flash))]
    [HarmonyPostfix]
    public static void FlashPostfix(NHandCardHolder __instance)
    {
        ApplyTemporaryHighlight(__instance);
    }

    private static void ApplyTemporaryHighlight(NHandCardHolder holder)
    {
        var cardNode = holder.CardNode;
        var card = cardNode?.Model;
        if (!HasTemporaryKeyword(card))
            return;

        var highlight = cardNode!.CardHighlight;

        // Preserve stronger built-in warning/recommendation colors when the game already chose one.
        if (highlight.Visible && highlight.Modulate != NCardHighlight.playableColor)
            return;

        highlight.Modulate = TemporaryHighlightColor;
        highlight.AnimShow();
    }

    private static bool HasTemporaryKeyword(CardModel? card)
    {
        return card != null && card.Keywords.Contains(AstralKeywords.AstralTemporary);
    }
}
