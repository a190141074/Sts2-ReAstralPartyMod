using System;
using System.Reflection;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class PersonaRelicHelper
{
    public static bool IsPersonaSkillCard(CardModel card)
    {
        return card.Type == CardType.Skill && AstralPartyCardModel.ShouldAutoApplyCooldown(card);
    }

    public static void AdvanceCooldownRelics(Player owner, int amount)
    {
        if (amount <= 0)
            return;

        foreach (var relic in owner.Relics)
            switch (relic)
            {
                case CooldownPersonaRelicBase cooldownRelic:
                    cooldownRelic.AdvanceCooldownProgressFromExternalEffect(amount);
                    break;
                case PersonBionicJasmine bionicJasmine:
                    bionicJasmine.AddSteps(amount);
                    break;
            }
    }

    public static void RefreshRelicDisplayAmount(object relic)
    {
        for (var currentType = relic.GetType(); currentType != null; currentType = currentType.BaseType)
        {
            var refreshMethod = currentType.GetMethod(
                "InvokeDisplayAmountChanged",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            if (refreshMethod == null)
                continue;

            refreshMethod.Invoke(relic, null);
            return;
        }
    }
}
