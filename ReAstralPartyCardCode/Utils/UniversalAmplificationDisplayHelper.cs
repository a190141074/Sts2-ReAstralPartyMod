using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class UniversalAmplificationDisplayHelper
{
    public static decimal GetStableAttackAmplification(Player? player)
    {
        if (player?.Creature == null)
            return 0m;

        var creature = player.Creature;
        decimal total = 0m;

        total += creature.GetPowerAmount<ChannelEnergyAttackBoostPower>();
        total += creature.GetPowerAmount<BaiZeBlessingPower>() > 0m
            ? creature.GetPowerAmount<HalfLifeHealPower>()
            : 0m;
        total += GetSwordAuraAttackBonus(creature);
        total += MosesCombatHelper.GetEquivalentAttackBonus(player);
        total += creature.GetPowerAmount<TrueDragonFormPower>() > 0m ? 4m : 0m;

        if (player.GetRelic<TokenPurpleBasicScope>() != null)
            total += 2m;
        if (player.GetRelic<TokenGoldArtKnifeSharp>() != null && ArtKnifeActivationHelper.IsActivationSatisfied(creature))
            total += creature.GetPowerAmount<HalfLifeHealPower>();

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

    public static async Task RefreshAmplificationDisplayPowers(Player? player, AbstractModel? source = null)
    {
        if (player?.Creature == null)
            return;

        var creature = player.Creature;
        var sourceCreature = creature;
        var sourceCard = source as CardModel;

        await RefreshSinglePower<AttackAmplificationDisplayPower>(
            creature,
            GetStableAttackAmplification(player),
            sourceCreature,
            sourceCard);
        await RefreshSinglePower<SkillAmplificationDisplayPower>(
            creature,
            GetStableSkillAmplification(player),
            sourceCreature,
            sourceCard);
    }

    private static async Task RefreshSinglePower<TPower>(
        Creature creature,
        decimal amount,
        Creature applier,
        CardModel? sourceCard)
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
            await PowerCmd.Apply<TPower>(creature, amount, applier, sourceCard, true);
            return;
        }

        if (existingPower.Amount != amount)
            await PowerCmd.SetAmount<TPower>(creature, amount, applier, sourceCard);
    }

    private static decimal GetSwordAuraAttackBonus(Creature creature)
    {
        var amount = creature.GetPowerAmount<SwordAuraPower>();
        if (amount >= 3m)
            return 4m;
        if (amount >= 2m)
            return 2m;
        if (amount >= 1m)
            return 1m;

        return 0m;
    }
}
