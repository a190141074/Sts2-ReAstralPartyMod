using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public sealed class LingHunLianJiePower : AstralPartyPowerModel
{
    private static int _propagationDepth;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!ShouldPropagate(dealer, result, target, cardSource))
            return;

        var propagatedDamage = StableNumericStateHelper.FloorToNonNegativeInt(result.TotalDamage * 0.5m);
        if (propagatedDamage <= 0)
            return;

        var otherEnemies = GetOtherLivingEnemies();
        if (otherEnemies.Count == 0)
            return;

        Flash();
        _propagationDepth++;
        try
        {
            foreach (var enemy in otherEnemies)
                await CreatureCmd.Damage(
                    choiceContext,
                    enemy,
                    propagatedDamage,
                    ValueProp.Unpowered | ValueProp.SkipHurtAnim,
                    dealer,
                    null);
        }
        finally
        {
            _propagationDepth--;
        }
    }

    private bool ShouldPropagate(
        Creature? dealer,
        DamageResult result,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return false;
        if (_propagationDepth > 0)
            return false;
        if (dealer == null || dealer.Side == Owner.Side)
            return false;
        if (result.TotalDamage <= 0m)
            return false;

        return IsExplicitSingleTargetSource(cardSource);
    }

    private static bool IsExplicitSingleTargetSource(CardModel? cardSource)
    {
        return cardSource != null && cardSource.TargetType == TargetType.AnyEnemy;
    }

    private List<Creature> GetOtherLivingEnemies()
    {
        if (Owner == null)
            return [];

        return CombatTargetOrdering.GetLivingOpponentsStable(Owner)
            .Where(enemy => enemy != Owner)
            .ToList();
    }
}
