using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class MidnightFlashHelper
{
    public static bool IsMudTruckCard(CardModel? card)
    {
        if (card == null)
            return false;

        var canonicalId = (card.CanonicalInstance ?? card).Id;
        return canonicalId == ModelDb.Card<SkillMudTruckCrash>().Id;
    }

    public static CardModel? CreateMudTruckCard(Player owner)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        return combatState.CreateCard(ModelDb.Card<SkillMudTruckCrash>(), owner);
    }
}
