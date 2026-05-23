using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class MosesCombatHelper
{
    private const int MaxWeaknessInsightStacks = 4;
    private const int MaxNodeValue = 6;
    private const int ExposedFlawUpperBound = 6;

    public static int GetWeaknessInsightAmount(Player? owner)
    {
        return owner?.Creature == null
            ? 0
            : Math.Max(0, (int)owner.Creature.GetPowerAmount<WeaknessInsightPower>());
    }

    public static int GetEquivalentAttackBonus(Player? owner)
    {
        return GetWeaknessInsightAmount(owner) * 2;
    }

    public static int GetExposedFlawUpperBound(Player? owner)
    {
        return Math.Max(0, ExposedFlawUpperBound - GetWeaknessInsightAmount(owner));
    }

    public static int RollExposedFlawAmount(Player owner, Creature target, CardModel sourceCard)
    {
        var upperBound = GetExposedFlawUpperBound(owner);
        if (upperBound <= 0)
            return 0;

        var creatures = owner.Creature?.CombatState?.Creatures;
        var targetIndex = -1;
        if (creatures != null)
            for (var i = 0; i < creatures.Count; i++)
                if (ReferenceEquals(creatures[i], target))
                {
                    targetIndex = i;
                    break;
                }

        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            1,
            upperBound + 1,
            MainFile.ModId,
            sourceCard.Id.Entry,
            nameof(ExposedFlawPower),
            owner.RunState?.Rng?.StringSeed,
            owner.NetId,
            owner.Creature?.CombatState?.RoundNumber ?? 0,
            targetIndex);
    }

    public static async Task DecayWeaknessInsightAtTurnEnd(Player? owner)
    {
        if (owner?.Creature == null)
            return;

        var power = owner.Creature.GetPower<WeaknessInsightPower>();
        if (power == null || power.Amount <= 0m)
            return;

        if (power.Amount <= 1m)
        {
            await PowerCmd.Remove(power);
            return;
        }

        await PowerCmd.Decrement(power);
    }

    public static int RollDodgeNodeValue(Player owner, AbstractModel source)
    {
        var minNodeValue = Math.Min(MaxNodeValue, GetWeaknessInsightAmount(owner) + 1);
        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            minNodeValue,
            MaxNodeValue + 1,
            MainFile.ModId,
            source.Id.Entry,
            nameof(DodgeStancePower),
            owner.RunState?.Rng?.StringSeed,
            owner.NetId,
            owner.Creature?.CombatState?.RoundNumber ?? 0);
    }

    public static int GetWeaknessAnalysisTurnLimit(Player? owner)
    {
        if (owner?.Creature?.CombatState == null)
            return 0;

        return owner.Creature.CombatState.Creatures.Count(creature =>
            creature.IsAlive && creature.Side != owner.Creature.Side);
    }

    public static int GetCurrentNodeAmount(Player? owner)
    {
        return owner?.Creature == null
            ? 0
            : Math.Max(0, (int)owner.Creature.GetPowerAmount<MosesNodePower>());
    }

    public static async Task EnsureNodeCarrier(Player? owner)
    {
        if (owner?.Creature == null)
            return;

        if (!owner.Creature.HasPower<MosesNodePower>())
            await PowerCmd.SetAmount<MosesNodePower>(owner.Creature, 0m, owner.Creature, null);
    }

    public static async Task TryGainWeaknessInsight(Player? owner, AbstractModel source)
    {
        if (owner?.Creature == null)
            return;

        var currentAmount = GetWeaknessInsightAmount(owner);
        if (currentAmount >= MaxWeaknessInsightStacks)
            return;

        var nextAmount = Math.Min(MaxWeaknessInsightStacks, currentAmount + 1);
        await PowerCmd.SetAmount<WeaknessInsightPower>(owner.Creature, nextAmount, owner.Creature, null);
    }

    public static async Task ReplaceStance(Player owner, CardModel sourceCard, bool chooseDodge)
    {
        if (owner.Creature == null)
            return;

        var creature = owner.Creature;
        var existingDefense = creature.GetPower<DefenseStancePower>();
        if (existingDefense != null)
            await PowerCmd.Remove(existingDefense);

        var existingDodge = creature.GetPower<DodgeStancePower>();
        if (existingDodge != null)
            await PowerCmd.Remove(existingDodge);

        if (chooseDodge)
            await PowerCmd.Apply<DodgeStancePower>(creature, 1m, creature, sourceCard, false);
        else
            await PowerCmd.Apply<DefenseStancePower>(creature, 1m, creature, sourceCard, false);
    }

    public static IReadOnlyList<CardModel> CreateStanceChoiceOptions(Player owner)
    {
        return
        [
            owner.Creature!.CombatState!.CreateCard(ModelDb.Card<SkillWeaknessAnalysisChooseDefense>(), owner),
            owner.Creature!.CombatState!.CreateCard(ModelDb.Card<SkillWeaknessAnalysisChooseDodge>(), owner)
        ];
    }

    public static bool HasExposedFlaw(Creature? target)
    {
        return target?.HasPower<ExposedFlawPower>() == true;
    }
}
