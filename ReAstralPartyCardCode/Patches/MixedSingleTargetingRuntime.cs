using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

internal static class MixedSingleTargetingRuntime
{
    private static readonly HashSet<int> MarkedCardInstanceIds = [];

    public static void MarkCardForDualTargetUi(CardModel? card)
    {
        if (card == null)
            return;

        MarkedCardInstanceIds.Add(card.GetHashCode());
    }

    public static bool IsMixedSingleTargetCard(CardModel? card)
    {
        if (card == null)
            return false;

        if (card is SkillFortuneMischance)
            return true;

        return card.CanonicalInstance?.Id == ModelDb.GetId<SkillFortuneMischance>()
               || MarkedCardInstanceIds.Contains(card.GetHashCode());
    }

    public static bool IsValidTarget(Creature? target)
    {
        return target is { IsAlive: true, IsHittable: true };
    }

    public static List<Creature> GetCandidates(CardModel card)
    {
        var combatState = card.CombatState ?? card.Owner?.Creature?.CombatState;
        var owner = card.Owner?.Creature;
        if (combatState == null || owner == null)
            return [];

        return combatState.PlayerCreatures
                .Where(IsValidTarget)
            .Concat(combatState.GetOpponentsOf(owner).Where(IsValidTarget))
            .Distinct()
            .ToList();
    }
}
