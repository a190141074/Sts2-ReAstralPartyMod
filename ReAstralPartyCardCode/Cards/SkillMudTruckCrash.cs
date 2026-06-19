using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillMudTruckCrash : AstralPartyCardModel
{
    private const decimal FractureAmount = 1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(11m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FracturePower>()
    ];

    public SkillMudTruckCrash() : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        ArgumentNullException.ThrowIfNull(Owner?.Creature);

        await using var context = await AttackCommand.CreateContextAsync(CombatState!, choiceContext, this);
        var primaryHits = (await CreatureCmd.Damage(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Damage.BaseValue,
            ValueProp.Move,
            this)).ToList();

        context.AddHit(primaryHits);
        if (cardPlay.Target.IsAlive)
            await PowerCmd.Apply<FracturePower>(cardPlay.Target, FractureAmount, Owner.Creature, this, false);

        var primaryHit = primaryHits.FirstOrDefault();
        if (primaryHit == null)
            return;

        var splashTargets = CombatState!
            .GetTeammatesOf(primaryHit.Receiver)
            .Except([cardPlay.Target])
            .Where(enemy => enemy.IsHittable)
            .ToList();

        if (splashTargets.Count == 0)
            return;

        var splashDamage = primaryHit.TotalDamage + primaryHit.OverkillDamage;
        context.AddHit(await CreatureCmd.Damage(
            choiceContext,
            splashTargets,
            splashDamage,
            ValueProp.Unpowered | ValueProp.Move,
            Owner.Creature,
            this));

        foreach (var splashTarget in splashTargets.Where(enemy => enemy.IsAlive))
            await PowerCmd.Apply<FracturePower>(splashTarget, FractureAmount, Owner.Creature, this, false);
    }
}

