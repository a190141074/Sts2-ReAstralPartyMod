using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class BoxingGlovesRelicHelper
{
    public static bool ShouldHandleSharedSet(RelicModel relic)
    {
        var owner = relic.Owner;
        if (owner == null)
            return false;

        return ReferenceEquals(owner.Relics.FirstOrDefault(IsBoxingGloveRelic), relic);
    }

    public static int GetSetCombatStartStrengthBonus(Player owner)
    {
        var ownedCount = CountOwnedBoxingGloves(owner);
        return ownedCount >= 3 ? 3 : ownedCount >= 2 ? 1 : 0;
    }

    private static int CountOwnedBoxingGloves(Player owner)
    {
        var count = 0;
        if (owner.GetRelic<TokenBlueBoxingGloveGeneral>() != null)
            count++;
        if (owner.GetRelic<TokenPurpleBoxingGloveIntermediate>() != null)
            count++;
        if (owner.GetRelic<TokenGoldBoxingGlovePremium>() != null)
            count++;

        return count;
    }

    private static bool IsBoxingGloveRelic(RelicModel relic)
    {
        return relic is TokenBlueBoxingGloveGeneral
               || relic is TokenPurpleBoxingGloveIntermediate
               || relic is TokenGoldBoxingGlovePremium;
    }
}