using System;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using ReAstralPartyMod.ReAstralPartyCardCode.TopBar;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.TopBar;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

[HarmonyPatch(typeof(NTopBar), nameof(NTopBar.Initialize))]
public static class AstralContentModeTopBarButtonPlacementPatch
{
    [HarmonyPostfix]
    public static void Postfix(NTopBar __instance, IRunState runState)
    {
        var container = ModTopBarLayout.GetRightAlignedContainer(__instance);
        var map = __instance.GetNodeOrNull<Control>("%Map");
        if (container == null || map == null)
            return;

        Node anchor = map;
        while (anchor.GetParent() is { } parent && parent != container)
            anchor = parent;
        if (anchor.GetParent() != container)
            return;

        foreach (var button in container.GetChildren().OfType<NModCardPileButton>())
        {
            if (!button.IsActionMode)
                continue;
            if (!string.Equals(
                    button.ActionDefinition?.Id,
                    AstralContentModeTopBarButtonHandler.ButtonId,
                    StringComparison.Ordinal))
                continue;

            var anchorIndex = anchor.GetIndex();
            var currentIndex = button.GetIndex();
            var targetIndex = currentIndex < anchorIndex ? anchorIndex - 1 : anchorIndex;
            if (currentIndex != targetIndex)
                container.MoveChild(button, targetIndex);
            return;
        }
    }
}

