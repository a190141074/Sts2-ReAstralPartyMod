using System;
using System.Reflection;
using AstralPartyMod.AstralPartyCardCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch]
public static class PersonShadowScionCardGainPatch
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
    public static void Prefix(CardModel card, [HarmonyArgument("newPileType")] PileType pileType)
    {
        if (pileType != PileType.Hand)
            return;

        var recipient = card.Owner;
        if (recipient?.Creature?.CombatState == null)
            return;

        foreach (var player in recipient.Creature.CombatState.Players)
        {
            var relic = player.GetRelic<PersonShadowScion>();
            if (relic == null)
                continue;

            relic.HandleObservedCardGain(recipient, card).GetAwaiter().GetResult();
        }
    }
}
