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

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => Math.Max(0, (int)Amount);

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

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (Owner == null || side != Owner.Side || Amount <= 0m)
            return;

        Flash();
        if (Amount <= 1m)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(this, -1m, Owner, null, true);
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
        var owner = Owner;
        var combatState = owner?.CombatState;
        if (owner == null || combatState == null)
            return [];

        return combatState.Creatures
            .Where(creature =>
                creature.IsAlive
                && creature != owner
                && creature.Side == owner.Side
                && creature.GetPower<LingHunLianJiePower>() != null)
            .OrderBy(creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(creature => creature.ModelId.ToString())
            .ThenBy(creature => creature.SlotName ?? string.Empty)
            .ToList();
    }
}
