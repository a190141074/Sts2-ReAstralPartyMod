using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Combat.CardTargeting;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CandyPotionTargetingHelper
{
    public static readonly TargetType AnyModifiedPlayer = CustomTargetType.RegisterSingleTargetType(
        MainFile.ModId,
        "any_modified_player",
        static creature => IsModifiedPlayer(creature));

    public static bool IsModifiedPlayer(Creature? creature)
    {
        return creature is { IsAlive: true, IsPet: false, Player: not null }
               && creature.GetPowerAmount<ModificationPower>() > 0m;
    }

    public static bool AnyModifiedPlayersInCombat(Player? owner)
    {
        return owner?.Creature?.CombatState?.Players.Any(player => IsModifiedPlayer(player.Creature)) == true;
    }
}
