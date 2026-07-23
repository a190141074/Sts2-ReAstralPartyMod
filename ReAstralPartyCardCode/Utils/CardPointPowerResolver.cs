using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class CardPointPowerResolver
{
    public static CardPointPowerBase? ResolveHighestPriorityPointPower(Creature? owner, CardModel? card)
    {
        if (owner == null || card == null)
            return null;

        var attackPoint = owner.GetPower<AttackPointPower>();
        if (attackPoint?.CanResolveForCard(card) == true)
            return attackPoint;

        var defendPoint = owner.GetPower<DefendPointPower>();
        if (defendPoint?.CanResolveForCard(card) == true)
            return defendPoint;

        var skillPoint = owner.GetPower<SkillPointPower>();
        if (skillPoint?.CanResolveForCard(card) == true)
            return skillPoint;

        return null;
    }
}
