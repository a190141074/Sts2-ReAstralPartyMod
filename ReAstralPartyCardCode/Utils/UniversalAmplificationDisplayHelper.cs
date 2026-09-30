using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class UniversalAmplificationDisplayHelper
{
    private static readonly AsyncLocal<int> RefreshDepth = new();
    public static decimal GetStableAttackAmplification(Player? player)
    {
        if (player?.Creature == null)
            return 0m;

        var creature = player.Creature;
        decimal total = 0m;

        total += creature.GetPowerAmount<ChannelEnergyAttackBoostPower>();
        total += creature.GetPowerAmount<BaiZeBlessingPower>() > 0m
            ? creature.GetPowerAmount<EffectsHealPower>()
            : 0m;
        total += GetSwordGustAttackBonus(creature);
        total += MosesCombatHelper.GetEquivalentAttackBonus(player);
        total += creature.GetPowerAmount<TrueDragonFormPower>() > 0m ? 4m : 0m;

        if (player.GetRelic<TokenPurpleBasicScope>() != null)
            total += 2m;
        if (player.GetRelic<TokenGoldArtKnifeSharp>() != null && ArtKnifeActivationHelper.IsActivationSatisfied(creature))
            total += creature.GetPowerAmount<EffectsHealPower>();

        return Math.Max(0m, total);
    }

    public static decimal GetStableSkillAmplification(Player? player)
    {
        if (player?.Creature == null)
            return 0m;

        var creature = player.Creature;
        decimal total = 0m;

        total += creature.GetPowerAmount<EsotericEmpowerPower>();
        total += creature.GetPowerAmount<SurveyFindsPower>() * 3m;

        if (player.GetRelic<TokenExclusiveAncientWand>() != null)
            total += 3m;

        return Math.Max(0m, total);
    }

    public static async Task RefreshAmplificationDisplayPowers(
        Player? player,
        AbstractModel? source = null,
        PlayerChoiceContext? choiceContext = null)
    {
        if (player?.Creature == null)
            return;

        // Display powers are themselves PowerCmd targets. Do not let their own
        // Apply/ModifyAmount callbacks start another refresh before this one ends.
        if (RefreshDepth.Value > 0)
            return;

        RefreshDepth.Value++;
        try
        {
            var creature = player.Creature;
            var sourceCreature = creature;
            var sourceCard = source as CardModel;

            await RefreshSinglePower<AttackAmplificationDisplayPower>(
                creature,
                GetStableAttackAmplification(player),
                sourceCreature,
                sourceCard,
                choiceContext);
            await RefreshSinglePower<SkillAmplificationDisplayPower>(
                creature,
                GetStableSkillAmplification(player),
                sourceCreature,
                sourceCard,
                choiceContext);
        }
        finally
        {
            RefreshDepth.Value--;
        }
    }

    private static async Task RefreshSinglePower<TPower>(
        Creature creature,
        decimal amount,
        Creature applier,
        CardModel? sourceCard,
        PlayerChoiceContext? choiceContext)
        where TPower : PowerModel
    {
        var existingPower = creature.GetPower<TPower>();
        if (amount <= 0m)
        {
            if (existingPower != null)
                await PowerCmd.Remove(existingPower);

            return;
        }

        if (existingPower == null)
        {
            if (choiceContext == null)
                await PowerCmd.Apply<TPower>(creature, amount, applier, sourceCard, true);
            else
                await PowerCmd.Apply<TPower>(choiceContext, creature, amount, applier, sourceCard, true);
            return;
        }

        if (existingPower.Amount != amount)
        {
            if (choiceContext == null)
                await PowerCmd.SetAmount<TPower>(creature, amount, applier, sourceCard);
            else
                await PowerCmd.SetAmount<TPower>(choiceContext, creature, amount, applier, sourceCard);
        }
    }

    private static decimal GetSwordGustAttackBonus(Creature creature)
    {
        var amount = creature.GetPowerAmount<SwordGustPower>();
        if (amount >= 3m)
            return 5m;
        if (amount >= 2m)
            return 2m;
        if (amount >= 1m)
            return 1m;
        return 0m;
    }

}
