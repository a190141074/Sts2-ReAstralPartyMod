using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillLivingFolio : AstralPartyCardModel
{
    private const decimal BaseDamage = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Eternal, CardKeyword.Retain, AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override bool IsPlayable =>
        Owner != null
        && HasLivingEnemyTarget()
        && Owner.GetRelic<PersonalityDerivativeLivingFolio>()?.AstralParty_PersonalityDerivativeLivingFolioStacks >= 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(BaseDamage, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<SurveyFindsPower>()
    ];

    public SkillLivingFolio() : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
    {
        await base.AfterCardChangedPiles(card, oldPileType, source);
        if (card == this)
            SyncDisplayedDamage();
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        return card == this
            ? (PileType.Hand, CardPilePosition.Top)
            : (pileType, position);
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        if (card != this)
        {
            modifiedCost = originalCost;
            return false;
        }

        modifiedCost = GetCombatEnergyCost(originalCost);
        return modifiedCost != originalCost;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target;
        if (owner == null || ownerCreature == null || target == null || target.Side == ownerCreature.Side ||
            !target.IsAlive)
            return;
        if (owner.GetRelic<PersonalityDerivativeLivingFolio>() is not { } livingFolioRelic)
            return;
        if (!livingFolioRelic.TryConsume(1))
            return;

        if (owner.GetRelic<PersonDeityLin>() is not { } personDeityLin)
            return;

        var targetMarksBeforePlay = target.GetPowerAmount<MarkLockPower>();
        personDeityLin.RecordLivingFolioConsumption(1);
        SyncDisplayedDamage();

        await CreatureCmd.Damage(
            choiceContext,
            target,
            GetCurrentDamageAmount(),
            ValueProp.Move,
            ownerCreature,
            this);

        await PowerCmd.Apply<MarkLockPower>(target, 1m, ownerCreature, this, false);

        if (targetMarksBeforePlay >= 3m && personDeityLin.TryRefundLivingFolioEnergy())
            await PlayerCmd.GainEnergy(1m, owner);
    }

    private bool HasLivingEnemyTarget()
    {
        var combatState = Owner?.Creature?.CombatState;
        var ownerCreature = Owner?.Creature;
        if (combatState == null || ownerCreature == null)
            return false;

        return combatState
            .GetOpponentsOf(ownerCreature)
            .Any(creature => creature.IsAlive);
    }

    private decimal GetCurrentDamageAmount()
    {
        return BaseDamage + GetPermanentDamageBonus();
    }

    private decimal GetCombatEnergyCost(decimal originalCost)
    {
        if (GetPermanentDamageBonus() > 11m)
            return originalCost;

        return Math.Max(0m, originalCost - 1m);
    }

    private decimal GetPermanentDamageBonus()
    {
        return Owner?.GetRelic<PersonDeityLin>()?.GetLivingFolioPermanentDamageBonus() ?? 0;
    }

    private void SyncDisplayedDamage()
    {
        DynamicVars["Damage"].BaseValue = GetCurrentDamageAmount();
    }
}
