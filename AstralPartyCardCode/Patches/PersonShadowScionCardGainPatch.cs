using AstralPartyMod.AstralPartyCardCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace AstralPartyMod.AstralPartyCardCode.Patches;

[HarmonyPatch(
    typeof(CardPileCmd),
    nameof(CardPileCmd.AddGeneratedCardToCombat),
    [typeof(CardModel), typeof(PileType), typeof(bool)])]
public static class PersonShadowScionCardGainPatch
{
    [HarmonyPrefix]
    public static void Prefix(CardModel card, PileType pileType)
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
