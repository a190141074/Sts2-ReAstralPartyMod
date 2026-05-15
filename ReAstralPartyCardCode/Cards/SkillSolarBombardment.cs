using System;
using System.Linq;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillSolarBombardment : AstralPartyCardModel
{
    private const int BaseHitCount = 3;
    private const int ExtraHitDiscardStep = 2;
    private const int BaseDamage = 3;
    private const int DamageIncreaseCostStep = 3;
    private const int RailgunMaterialCount = 2;
    private const int RailgunMaterialCost = 3;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move | ValueProp.Unblockable),
        new RepeatVar(BaseHitCount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.SolarBombardmentId)
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
        var discardedCardCount = handCards.Sum(GetBombardmentMaterialCount);
        var discardedCostTotal = handCards.Sum(GetBombardmentDamageCost);
        var cardsToExhaust = handCards.Where(ShouldExhaustWhenBombarded).ToList();
        var cardsToDiscard = handCards.Where(card => !ShouldExhaustWhenBombarded(card)).ToList();

        if (cardsToDiscard.Count > 0)
            await CardCmd.Discard(choiceContext, cardsToDiscard);

        foreach (var card in cardsToExhaust)
            await CardCmd.Exhaust(choiceContext, card, false, false);

        var hitCount = BaseHitCount + discardedCardCount / ExtraHitDiscardStep;
        var damage = BaseDamage + discardedCostTotal / DamageIncreaseCostStep;
        if (hitCount <= 0 || damage <= 0)
            return;

        await FireBombardment(choiceContext, Owner, this, hitCount, damage);
    }

    private static int GetBombardmentMaterialCount(CardModel card)
    {
        return IsOrbitalRailgun(card) ? RailgunMaterialCount : 1;
    }

    private int GetBombardmentDamageCost(CardModel card)
    {
        return IsOrbitalRailgun(card) ? RailgunMaterialCost : GetEffectiveDiscardCost(card);
    }

    public static async Task FireBaseBombardment(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        CardModel? sourceCard,
        int hitCount = 1
    )
    {
        await FireBombardment(choiceContext, owner, sourceCard, hitCount, BaseDamage);
    }

    public static async Task FireBombardment(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player owner,
        CardModel? sourceCard,
        int hitCount,
        int damage
    )
    {
        if (owner.Creature?.CombatState == null || hitCount <= 0 || damage <= 0)
            return;

        for (var i = 0; i < hitCount; i++)
        {
            var targets = CombatTargetOrdering.GetLivingOpponentsStable(owner.Creature);
            if (targets.Count == 0)
                break;

            var target = targets[owner.RunState.Rng.CombatTargets.NextInt(targets.Count)];
            if (sourceCard != null)
                await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move | ValueProp.Unblockable,
                    owner.Creature, sourceCard);
            else
                await CreatureCmd.Damage(choiceContext, target, damage, ValueProp.Move | ValueProp.Unblockable,
                    owner.Creature);
        }
    }

    private int GetEffectiveDiscardCost(CardModel card)
    {
        if (card.EnergyCost.CostsX)
            return Owner?.PlayerCombatState?.Energy ?? 0;

        return Math.Max(1, card.EnergyCost.GetResolved());
    }

    private static bool ShouldExhaustWhenBombarded(CardModel card)
    {
        return IsOrbitalRailgun(card);
    }

    private static bool IsOrbitalRailgun(CardModel card)
    {
        return card is BaseAbilityOrbitalRailgun;
    }
}
