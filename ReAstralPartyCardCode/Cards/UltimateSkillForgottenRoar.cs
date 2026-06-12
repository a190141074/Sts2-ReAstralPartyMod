using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
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

    public UltimateSkillForgottenRoar() : base(2, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null || CombatState == null)
            return;
        if (CurrentCharge < UltimateSkillChargeHelper.MaxCharge)
        {
            UltimateSkillChargeHelper.SuppressNextUltimateChargeReset(this);

            foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(CombatState, Owner.Creature))
                await CreatureCmd.Stun(enemy);

            foreach (var player in EventCombatTargetHelper.GetAlivePlayers(CombatState))
                await PowerCmd.Apply<RingingPower>(player.Creature, 1m, Owner.Creature, this);

            return;
        }

        if (!TryConsumeFullCharge())
            return;

        await PowerCmd.Apply<IronVirginWardPower>(Owner.Creature, 1m, Owner.Creature, this, false);

        var enemies = EventCombatTargetHelper.GetAliveNonSummonEnemies(CombatState, Owner.Creature);

        foreach (var enemy in enemies)
            await CreatureCmd.Stun(enemy);

        foreach (var enemy in enemies)
            for (var i = 0; i < HitCount; i++)
                await CreatureCmd.Damage(choiceContext, enemy, DamagePerHit, ValueProp.Move, Owner.Creature, this);

        PlayerCmd.EndTurn(Owner, canBackOut: false);
    }
}
