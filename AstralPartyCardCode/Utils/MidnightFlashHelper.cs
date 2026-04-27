using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class MidnightFlashHelper
{
    public static bool IsWhirlwindCard(CardModel? card)
    {
        if (card == null)
            return false;

        var canonicalId = (card.CanonicalInstance ?? card).Id;
        return canonicalId == ModelDb.Card<Whirlwind>().Id;
    }

    public static CardModel? CreateWhirlwindCard(Player owner)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        var whirlwind = combatState.CreateCard(ModelDb.Card<Whirlwind>(), owner);
        if (owner.RunState.Rng.Niche.NextInt(2) == 0)
            CardCmd.Upgrade(whirlwind);

        return whirlwind;
    }
}
