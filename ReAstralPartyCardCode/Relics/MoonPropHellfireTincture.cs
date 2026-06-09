using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropHellfireTincture : MoonPropStackableRelicBase
{
    private const decimal BaseMaxHpRatio = 0.025m;
    private const decimal AllyRatio = 0.25m;
    private const decimal EnemyMultiplier = 12m;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new StringVar("SelfDamagePercent", GetSelfDamagePercentText()),
        new StringVar("AllyDamagePercent", GetAllyDamagePercentText()),
        new StringVar("EnemyMultiplier", GetEnemyMultiplierText())
    ];

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var ownerCreature = Owner.Creature;
        var stacks = GetStacks();
        var baseDamage = GetRoundedUpDamage(ownerCreature.MaxHp * BaseMaxHpRatio * stacks);
        if (baseDamage <= 0m)
            return;

        var allyDamage = GetRoundedUpDamage(baseDamage * AllyRatio * stacks);
        var enemyDamage = baseDamage * GetEnemyDamageMultiplier(stacks);

        Flash();

        await CreatureCmd.Damage(
            choiceContext,
            ownerCreature,
            baseDamage,
            ValueProp.Unpowered,
            ownerCreature,
            null);

        foreach (var ally in ownerCreature.CombatState
                     .GetTeammatesOf(ownerCreature)
                     .Where(static creature => creature.IsAlive))
        {
            if (allyDamage <= 0m)
                break;

            await CreatureCmd.Damage(
                choiceContext,
                ally,
                allyDamage,
                ValueProp.Unpowered,
                ownerCreature,
                null);
        }

        foreach (var enemy in ownerCreature.CombatState
                     .GetOpponentsOf(ownerCreature)
                     .Where(static creature => creature.IsAlive))
        {
            await CreatureCmd.Damage(
                choiceContext,
                enemy,
                enemyDamage,
                ValueProp.Unpowered,
                ownerCreature,
                null);
        }
    }

    private static decimal GetRoundedUpDamage(decimal rawDamage)
    {
        return rawDamage <= 0m ? 0m : Math.Ceiling(rawDamage);
    }

    private static decimal GetEnemyDamageMultiplier(int stacks)
    {
        return EnemyMultiplier + Math.Max(0, stacks - 1);
    }

    private string GetSelfDamagePercentText()
    {
        return FormatPercent(BaseMaxHpRatio * GetStacks());
    }

    private string GetAllyDamagePercentText()
    {
        return FormatPercent(AllyRatio * GetStacks());
    }

    private string GetEnemyMultiplierText()
    {
        return FormatValue(GetEnemyDamageMultiplier(GetStacks()));
    }

    protected override void RefreshDynamicState()
    {
        SetDynamicString("SelfDamagePercent", GetSelfDamagePercentText());
        SetDynamicString("AllyDamagePercent", GetAllyDamagePercentText());
        SetDynamicString("EnemyMultiplier", GetEnemyMultiplierText());
    }
}
