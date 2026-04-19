using System;
using System.Linq;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class SkillSolarBombardment : AstralPartyCardModel
{
    private const int BaseHitCount = 3;
    private const int ExtraHitDiscardStep = 2;
    private const int BaseDamage = 3;
    private const int DamageIncreaseCostStep = 3;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Unblockable),
        new RepeatVar(BaseHitCount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(AstralKeywords.SolarBombardment)
    ];

    public SkillSolarBombardment() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.RandomEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || CombatState == null)
            return;

        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        var discardedCardCount = handCards.Count;
        var discardedCostTotal = handCards.Sum(GetEffectiveDiscardCost);

        if (discardedCardCount > 0)
            await CardCmd.Discard(choiceContext, handCards);

        var hitCount = BaseHitCount + discardedCardCount / ExtraHitDiscardStep;
        var damage = BaseDamage + discardedCostTotal / DamageIncreaseCostStep;
        if (hitCount <= 0 || damage <= 0)
            return;

        for (var i = 0; i < hitCount; i++)
        {
            var target = Owner.RunState.Rng.CombatTargets.NextItem(
                CombatState.GetOpponentsOf(Owner.Creature).Where(creature => creature.IsAlive)
            );
            if (target == null)
                break;

            await CreatureCmd.Damage(
                choiceContext,
                target,
                damage,
                ValueProp.Move | ValueProp.Unblockable,
                Owner.Creature,
                this
            );
        }
    }

    private int GetEffectiveDiscardCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return Owner?.PlayerCombatState?.Energy ?? 0;

        return Math.Max(1, card.EnergyCost.GetResolved());
    }
}