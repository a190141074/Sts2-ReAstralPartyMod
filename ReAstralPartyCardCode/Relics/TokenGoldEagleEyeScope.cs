using System.Collections.Generic;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class TokenGoldEagleEyeScope : AstralPartyRelicModel
{
    private readonly Dictionary<CardModel, Creature> _pursuitTargetsByCard = [];

    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>()
    ];

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (!IsTrackedAttack(target, result.TotalDamage, dealer, cardSource))
            return;
        if (result.TotalDamage <= 0m)
            return;

        await PowerCmd.Apply<MarkLockPower>(target, 1m, Owner?.Creature, cardSource, false);

        _pursuitTargetsByCard.TryAdd(cardSource!, target);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_pursuitTargetsByCard.Remove(cardPlay.Card, out var target))
            return;
        if (Owner?.Creature == null || !target.IsAlive)
            return;

        var markedStacks = target.GetPowerAmount<MarkLockPower>();
        var pursuitDamage = StableNumericStateHelper.FloorDivisionToNonNegativeInt(markedStacks, 2m) * 2m;
        if (pursuitDamage <= 0m)
            return;

        await CreatureCmd.Damage(
            choiceContext,
            target,
            pursuitDamage,
            ValueProp.Unpowered,
            Owner?.Creature,
            null);
    }

    private bool IsTrackedAttack(Creature? target, decimal amount, Creature? dealer, CardModel? cardSource)
    {
        if (Owner?.Creature == null)
            return false;
        if (dealer != Owner.Creature)
            return false;
        if (target == null || target.Side == Owner.Creature.Side)
            return false;
        if (amount <= 0m)
            return false;
        if (cardSource?.Owner != Owner)
            return false;
        if (!FlashlightRelicHelper.IsTrackedAttackCard(Owner, cardSource, 1))
            return false;

        return true;
    }
}
