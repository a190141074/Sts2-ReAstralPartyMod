using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class AstralSinkouHelper
{
    private const decimal BaseAttackBonusRatio = 0.075m;
    private const decimal AttackBonusPerBurnRatio = 0.04m;
    private const decimal MaxAttackBonusRatio = 0.475m;
    private const decimal PunitiveJudgmentBonusPerBurnRatio = 0.03m;
    private const decimal PunitiveJudgmentBonusMaxHpRatio = 0.11m;
    private const decimal PunitiveJudgmentBossCapRatio = 1.4m;
    private const int RageDuration = 3;

    public static async Task EnsureAfterglow(Player? owner)
    {
        if (owner == null || owner.GetRelic<PersonalityDerivativeDawnAndDuskAfterglow>() != null)
            return;

        await RelicCmd.Obtain(ModelDb.Relic<PersonalityDerivativeDawnAndDuskAfterglow>().ToMutable(), owner);
    }

    public static decimal GetAttackBonusAmount(Player? owner, decimal amount)
    {
        if (owner?.Creature?.CombatState == null || amount <= 0m)
            return 0m;

        var ratio = Math.Min(MaxAttackBonusRatio, BaseAttackBonusRatio + GetTotalEnemyBurnStacks(owner) * AttackBonusPerBurnRatio);
        if (ratio <= 0m)
            return 0m;

        return amount * ratio;
    }

    public static int GetTotalEnemyBurnStacks(Player? owner)
    {
        if (owner?.Creature?.CombatState == null || owner.Creature == null)
            return 0;

        return owner.Creature.CombatState
            .GetOpponentsOf(owner.Creature)
            .Where(enemy => enemy.IsAlive)
            .Sum(enemy => GetRoundedPowerAmount(enemy.GetPowerAmount<BlazingSolarBurnPower>()));
    }

    public static int GetTargetBurnStacks(Creature? target)
    {
        return target == null ? 0 : GetRoundedPowerAmount(target.GetPowerAmount<BlazingSolarBurnPower>());
    }

    public static decimal GetPunitiveJudgmentDamageMultiplier(Creature? target)
    {
        return 1m + GetTargetBurnStacks(target) * PunitiveJudgmentBonusPerBurnRatio;
    }

    public static decimal GetPunitiveJudgmentUnblockableDamage(Player owner, Creature target)
    {
        var rawDamage = Math.Ceiling(target.MaxHp * PunitiveJudgmentBonusMaxHpRatio);
        if (target.CombatState?.Encounter?.RoomType != RoomType.Boss)
            return rawDamage;

        var bossCap = Math.Ceiling(owner.Creature.MaxHp * PunitiveJudgmentBossCapRatio);
        return Math.Min(rawDamage, bossCap);
    }

    public static async Task ApplyOrRefreshRageToAllEnemies(Player owner, AbstractModel source)
    {
        if (owner.Creature?.CombatState == null)
            return;

        var enemies = owner.Creature.CombatState
            .GetOpponentsOf(owner.Creature)
            .Where(enemy => enemy.IsAlive)
            .ToList();

        foreach (var enemy in enemies)
            await ApplyOrRefreshRage(enemy, owner.Creature, source as CardModel);
    }

    public static async Task ApplyOrRefreshRage(Creature target, Creature? applier, CardModel? cardSource)
    {
        var existing = target.GetPower<RageOfFirePower>();
        if (existing != null)
        {
            await PowerCmd.SetAmount<RageOfFirePower>(target, RageDuration, applier, cardSource);
            return;
        }

        await PowerCmd.Apply<RageOfFirePower>(target, RageDuration, applier, cardSource, false);
    }

    public static async Task GrantDeathBenefitsToTeammates(Player owner)
    {
        if (owner.Creature?.CombatState == null)
            return;

        var teammates = GetStableTeammates(owner)
            .Where(player => player.Creature != null && player.Creature.IsAlive)
            .ToList();

        foreach (var teammate in teammates)
        {
            await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), 2m, teammate);
            await PlayerCmd.GainEnergy(2m, teammate);
        }
    }

    public static IEnumerable<Player> GetStableTeammates(Player owner)
    {
        return owner.RunState.Players
            .Where(player => player != owner)
            .OrderBy(player => player.NetId);
    }

    public static int GetRoundedPowerAmount(decimal amount)
    {
        return Math.Max(0, Convert.ToInt32(decimal.Round(amount, 0, MidpointRounding.AwayFromZero)));
    }
}
