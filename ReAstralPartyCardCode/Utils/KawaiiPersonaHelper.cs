using System.Collections.Generic;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class KawaiiPersonaHelper
{
    public static IReadOnlyList<Player> GetSameSidePlayersWithRelic<TRelic>(Player? owner)
        where TRelic : AstralPartyRelicModel
    {
        var combatState = owner?.Creature?.CombatState;
        if (owner?.Creature == null || combatState == null)
            return [];

        return combatState.Players
            .Where(player => player.Creature != null)
            .Where(player => player.Creature.Side == owner.Creature.Side)
            .Where(player => player.GetRelic<TRelic>() != null)
            .OrderBy(player => player.NetId)
            .ToList();
    }

    public static bool IsIdolTarget(Creature? target)
    {
        var player = target?.Player ?? target?.PetOwner;
        if (player == null)
            return false;

        return player.GetRelic<PersonKawaiiAngel>() != null
               || player.GetRelic<PersonNeedyGirl>() != null;
    }
}
