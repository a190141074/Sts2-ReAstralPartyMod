using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Relics;

[RegisterRelic(typeof(SharedRelicPool))]
public class MoonPropHellfireTincture : AstralPartyRelicModel
{
    private const int DiscountedMerchantCost = 100;
    private const decimal BaseMaxHpRatio = 0.025m;
    private const decimal AllyRatio = 0.25m;
    private const decimal EnemyMultiplier = 12m;

    public override RelicRarity Rarity => RelicRarity.Shop;

    public override int MerchantCost => DiscountedMerchantCost;

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner || Owner?.Creature?.CombatState == null)
            return;

        var ownerCreature = Owner.Creature;
        var baseDamage = GetRoundedUpDamage(ownerCreature.MaxHp * BaseMaxHpRatio);
        if (baseDamage <= 0m)
            return;

        var allyDamage = GetRoundedUpDamage(baseDamage * AllyRatio);
        var enemyDamage = baseDamage * EnemyMultiplier;

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
}
