using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class VigilCounterContextPower : AstralPartyPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    protected override bool IsVisibleInternal => false;

    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || amount <= 0m)
            return 0m;
        if (!VigilCounterCombatHelper.IsCurrentVigilAttack(dealer, cardSource, Owner))
            return 0m;

        var bonus = JingRuiPower.GetVigilDamageBonus(Owner, amount)
                    + FlowerHiddenUnseenPower.GetVigilDamageBonus(target, amount);
        if (target?.Block > 0)
            bonus += amount * 0.25m;

        return bonus;
    }

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (Owner == null || autoPlayType != AutoPlayType.Default)
            return base.ShouldPlay(card, autoPlayType);

        return VigilCounterCombatHelper.IsCurrentVigilCard(card, Owner)
            ? true
            : base.ShouldPlay(card, autoPlayType);
    }
}
