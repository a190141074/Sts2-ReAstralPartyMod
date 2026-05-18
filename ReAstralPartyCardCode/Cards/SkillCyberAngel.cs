using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillCyberAngel : AstralPartyCardModel
{
    private const decimal BaseStarLightGain = 1m;
    private const decimal MaxStarLightGain = 10m;
    private const decimal RegenerationAmount = 2m;
    private const decimal ExtraTeamStarLightGain = 3m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<FanPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<RegenPower>()
    ];

    public SkillCyberAngel() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature == null || cardPlay.Target == null || cardPlay.Target.Side == Owner.Creature.Side)
            return;

        var target = cardPlay.Target;
        var fanStacksBeforeApply = Math.Max((int)target.GetPowerAmount<FanPower>(), 0);
        await PowerCmd.Apply<FanPower>(target, 1m, Owner.Creature, this, false);

        var starLightToGain = Math.Min(BaseStarLightGain + fanStacksBeforeApply, MaxStarLightGain);
        await PowerCmd.Apply(
            ModelDb.Power<StarLightPower>().ToMutable(),
            Owner.Creature,
            starLightToGain,
            Owner.Creature,
            this,
            false);

        var enemiesWithFans = Owner.Creature.CombatState?.Enemies
            .Where(enemy => enemy.IsAlive)
            .Where(enemy => enemy.GetPowerAmount<FanPower>() > 0m)
            .ToList();
        if (enemiesWithFans == null || enemiesWithFans.Count == 0)
            return;

        var kawaiiDerivative = Owner.GetRelic<PersonalityDerivativeKawaiiAngel>();
        if (kawaiiDerivative != null
            && enemiesWithFans.Sum(enemy => enemy.GetPowerAmount<FanPower>()) > 9m
            && kawaiiDerivative.TryConsumeTrigger())
        {
            foreach (var enemy in Owner.Creature.CombatState!.Enemies.Where(enemy => enemy.IsAlive))
                await PowerCmd.Apply<CyberAngelStrengthLossPower>(enemy, 1m, Owner.Creature, this, false);

            foreach (var player in Owner.Creature.CombatState.Players
                         .Where(player => player != Owner)
                         .OrderBy(player => player.NetId))
                await PowerCmd.Apply<StarLightPower>(player.Creature, ExtraTeamStarLightGain, Owner.Creature, this,
                    false);
        }

        if (enemiesWithFans.All(enemy => enemy.GetPowerAmount<FanPower>() > 3m))
            await PowerCmd.Apply<RegenPower>(Owner.Creature, RegenerationAmount, Owner.Creature, this, false);
    }
}

