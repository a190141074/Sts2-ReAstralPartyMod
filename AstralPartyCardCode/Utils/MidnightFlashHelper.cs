using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace AstralPartyMod.AstralPartyCardCode.Utils;

public static class MidnightFlashHelper
{
    public static bool IsOmnisliceCard(CardModel? card)
    {
        if (card == null)
            return false;

        var canonicalId = (card.CanonicalInstance ?? card).Id;
        return canonicalId == ModelDb.Card<Omnislice>().Id;
    }

    public static CardModel? CreateOmnisliceCard(Player owner)
    {
        var combatState = owner.Creature?.CombatState;
        if (combatState == null)
            return null;

        var omnislice = combatState.CreateCard(ModelDb.Card<Omnislice>(), owner);
        if (owner.RunState.Rng.Niche.NextInt(2) == 0)
            CardCmd.Upgrade(omnislice);

        return omnislice;
    }
}
