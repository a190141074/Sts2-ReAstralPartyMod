using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ConcealingPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<AmbushPower>()
    ];

    public override decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || target != Owner)
            return amount;
        if (amount <= 0m)
            return amount;
        if (!IsHostileSource(dealer, cardSource))
            return amount;

        Flash();
        return 0m;
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (Owner == null)
            return;
        if (dealer != Owner)
            return;
        if (target.Side == Owner.Side)
            return;
        if (result.TotalDamage <= 0m)
            return;

        var ambushAmount = target.GetPowerAmount<MarkLockPower>();
        Flash();
        if (ambushAmount > 0m)
            await PowerCmd.Apply<AmbushPower>(Owner, ambushAmount, Owner, cardSource, false);

        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (Owner == null || side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    private bool IsHostileSource(Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null)
            return dealer.Side != Owner?.Side;

        var cardOwnerCreature = cardSource?.Owner?.Creature;
        return cardOwnerCreature != null && cardOwnerCreature.Side != Owner?.Side;
    }
}
