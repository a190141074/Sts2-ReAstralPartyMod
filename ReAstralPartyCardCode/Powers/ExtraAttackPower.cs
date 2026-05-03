using System.Collections.Generic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Powers;

public class ExtraAttackPower : AstralPartyPowerModel
{
    private readonly HashSet<CardModel> _triggeredAttackCards = [];
    private CardModel? _activeTriggeredCard;
    private Creature? _activeTarget;
    private decimal _activeBonusDamage;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.None;

    public void BeginTriggeredAttack(CardModel card, Creature target, decimal bonusDamage)
    {
        _activeTriggeredCard = card;
        _activeTarget = target;
        _activeBonusDamage = bonusDamage;
        _triggeredAttackCards.Add(card);
    }

    public void EndTriggeredAttack(CardModel card)
    {
        _triggeredAttackCards.Remove(card);
        if (_activeTriggeredCard != card)
            return;

        _activeTriggeredCard = null;
        _activeTarget = null;
        _activeBonusDamage = 0m;
    }

    public bool IsTriggeredAttack(CardModel? card)
    {
        return card != null && _triggeredAttackCards.Contains(card);
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner == null || dealer != Owner)
            return 0m;
        if (cardSource == null || cardSource != _activeTriggeredCard)
            return 0m;
        if (target == null || target != _activeTarget)
            return 0m;

        return _activeBonusDamage;
    }
}
