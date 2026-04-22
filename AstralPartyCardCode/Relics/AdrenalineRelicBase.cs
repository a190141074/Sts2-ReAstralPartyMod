using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

public abstract class AdrenalineRelicBase : AstralPartyRelicModel
{
    protected abstract decimal HalfHpBonus { get; }
    protected abstract decimal QuarterHpBonus { get; }

    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return 0m;
        if (dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (cardSource?.Type != CardType.Attack)
            return 0m;
        if (Owner.Creature.MaxHp <= 0m)
            return 0m;

        var hpRatio = Owner.Creature.CurrentHp / Owner.Creature.MaxHp;
        if (hpRatio < 0.25m)
            return QuarterHpBonus;
        if (hpRatio < 0.5m)
            return HalfHpBonus;

        return 0m;
    }
}
