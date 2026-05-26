using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonaSkillCardPool))]
public class UltimateSkillForgottenRoar : UltimateSkillCardModel
{
    private const int HitCount = 13;
    private const int DamagePerHit = 6;

    protected override string CardId => "ultimate_skill_forgotten_roar";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        .. base.CanonicalVars,
        new DamageVar(DamagePerHit, ValueProp.Move),
        new RepeatVar(HitCount)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        .. base.ExtraHoverTips,
        StunIntent.GetStaticHoverTip(),
        HoverTipFactory.FromPower<IronVirginWardPower>()
    ];

    public UltimateSkillForgottenRoar() : base(2, CardType.Skill, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || CombatState == null)
            return;
        if (!TryConsumeFullCharge())
            return;

        await PowerCmd.Apply<IronVirginWardPower>(Owner.Creature, 1m, Owner.Creature, this, false);

        var enemies = CombatState.Creatures
            .Where(creature => creature.IsAlive && creature.Side != Owner.Creature.Side)
            .ToList();

        foreach (var enemy in enemies)
            await CreatureCmd.Stun(enemy);

        foreach (var enemy in enemies)
            for (var i = 0; i < HitCount; i++)
                await CreatureCmd.Damage(choiceContext, enemy, DamagePerHit, ValueProp.Move, Owner.Creature, this);

        var endTurnAction = new EndPlayerTurnAction(Owner, 0);
        await endTurnAction.Execute();
    }
}
