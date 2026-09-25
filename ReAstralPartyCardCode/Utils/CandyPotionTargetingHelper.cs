using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Context;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using STS2RitsuLib.Combat.CardTargeting;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

public static class CandyPotionTargetingHelper
{
    public static readonly TargetType AnyOtherLivingPlayer = CustomTargetType.RegisterSingleTargetType(
        MainFile.ModId,
        "any_other_living_player",
        static creature => IsOtherLivingPlayer(creature, null));

    public static readonly TargetType AnyModifiedPlayer = CustomTargetType.RegisterSingleTargetType(
        MainFile.ModId,
        "any_modified_player",
        static creature => IsModifiedPlayer(creature));

    public static bool IsModifiedPlayer(Creature? creature)
    {
        return creature is { IsAlive: true, IsPet: false, Player: not null }
               && creature.GetPowerAmount<ModificationPower>() > 0m;
    }

    public static bool IsOtherLivingPlayer(Creature? creature, Creature? owner)
    {
        return creature is { IsAlive: true, IsPet: false, Player: not null }
               && !ReferenceEquals(creature, owner)
               && !LocalContext.IsMe(creature.Player);
    }

    public static bool AnyModifiedPlayersInCombat(Player? owner)
    {
        return owner?.Creature?.CombatState?.Players.Any(player => IsModifiedPlayer(player.Creature)) == true;
    }
}
