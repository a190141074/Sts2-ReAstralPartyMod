using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.cards;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class FateFirewoodStickCombatHelper
{
    private const int MinNodeValue = 1;
    private const int MaxNodeValue = 6;

    public static bool HasAnyPlayableBranch(Player? owner)
    {
        return BaseAbilityHelper.HasOtherLivingPlayerTarget(owner) || HasLivingEnemyTarget(owner);
    }

    public static bool HasLivingEnemyTarget(Player? owner)
    {
        var ownerCreature = owner?.Creature;
        var combatState = ownerCreature?.CombatState;
        if (combatState == null || ownerCreature == null)
            return false;

        return EventCombatTargetHelper.GetAliveNonSummonEnemies(combatState, ownerCreature).Count > 0;
    }

    public static IReadOnlyList<CardModel> GetAvailableBranchOptions(Player owner)
    {
        return
        [
            ModelDb.Card<SkillFateFirewoodStickRen>(),
            ModelDb.Card<SkillFateFirewoodStickYi>()
        ];
    }

    public static bool IsFirewoodCard(CardModel? card)
    {
        return card is SkillFateFirewoodStick or SkillFateFirewoodStickRen or SkillFateFirewoodStickYi;
    }

    public static async Task ResolveDuelAsync(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        Creature target,
        VariantPersonManosabaLinHiro hiroRelic,
        bool shouldTransferOnSuccess)
    {
        var owner = sourceCard.Owner;
        var ownerCreature = owner?.Creature;
        if (owner == null || ownerCreature == null)
            return;

        var ownerNode = NodePowerRollHelper.RollNodeValue(owner, sourceCard, "fate_firewood_owner", MinNodeValue, MaxNodeValue);
        var targetNode = RollTargetNodeValue(owner, target, sourceCard, shouldTransferOnSuccess);

        await PowerCmd.SetAmount<FateFirewoodNodePower>(ownerCreature, ownerNode, ownerCreature, sourceCard);
        await PowerCmd.SetAmount<FateFirewoodNodePower>(target, targetNode, ownerCreature, sourceCard);

        try
        {
            if (ownerNode >= targetNode)
            {
                await ResolveSuccess(choiceContext, sourceCard, ownerCreature, target, hiroRelic, shouldTransferOnSuccess);
                return;
            }

            await ResolveFailure(choiceContext, sourceCard, ownerCreature, hiroRelic);
        }
        finally
        {
            await RemoveNodePower(ownerCreature);
            await RemoveNodePower(target);
        }
    }

    private static int RollTargetNodeValue(Player owner, Creature target, CardModel sourceCard, bool shouldTransferOnSuccess)
    {
        if (target.Player != null)
            return NodePowerRollHelper.RollNodeValue(target.Player, sourceCard, "fate_firewood_target_ally", MinNodeValue, MaxNodeValue);

        return DeterministicMultiplayerChoiceHelper.RollDeterministically(
            MinNodeValue,
            MaxNodeValue + 1,
            MainFile.ModId,
            sourceCard.Id.Entry,
            shouldTransferOnSuccess ? "fate_firewood_target_ally_fallback" : "fate_firewood_target_enemy",
            owner.RunState?.Rng?.StringSeed,
            owner.NetId,
            owner.Creature?.CombatState?.RoundNumber ?? 0,
            target.CombatId ?? uint.MaxValue,
            target.ModelId.ToString(),
            target.SlotName ?? string.Empty);
    }

    private static async Task ResolveSuccess(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        Creature ownerCreature,
        Creature target,
        VariantPersonManosabaLinHiro hiroRelic,
        bool shouldTransferOnSuccess)
    {
        var totalWithAmount = GetLivingCreaturesStable(ownerCreature.CombatState)
            .Sum(hiroRelic.GetWithPowerAmount);
        var successDamage = StableNumericStateHelper.FloorToNonNegativeInt(totalWithAmount * 0.5m);
        if (successDamage > 0)
        {
            await CreatureCmd.Damage(
                choiceContext,
                target,
                successDamage,
                ValueProp.Move,
                ownerCreature,
                sourceCard);
        }

        if (shouldTransferOnSuccess)
            await hiroRelic.TryTransferWithPowerAndRegainCard(target, sourceCard);
    }

    private static async Task ResolveFailure(
        PlayerChoiceContext choiceContext,
        CardModel sourceCard,
        Creature ownerCreature,
        VariantPersonManosabaLinHiro hiroRelic)
    {
        var failureDamage = StableNumericStateHelper.FloorToNonNegativeInt(hiroRelic.GetCurrentWithPower() * 0.1m);
        if (failureDamage <= 0)
            return;

        var candidates = GetLivingCreaturesStable(ownerCreature.CombatState);
        if (candidates.Count == 0)
            return;

        var selectedIndex = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            candidates.Count,
            MainFile.ModId,
            sourceCard.Id.Entry,
            "failure_target",
            sourceCard.Owner?.RunState?.Rng?.StringSeed,
            sourceCard.Owner?.NetId,
            ownerCreature.CombatState?.RoundNumber ?? 0,
            candidates.Count);
        var randomTarget = candidates[selectedIndex];
        await CreatureCmd.Damage(
            choiceContext,
            randomTarget,
            failureDamage,
            ValueProp.Move,
            ownerCreature,
            sourceCard);
    }

    private static async Task RemoveNodePower(Creature creature)
    {
        if (creature.GetPower<FateFirewoodNodePower>() is { } nodePower)
            await PowerCmd.Remove(nodePower);
    }

    private static List<Creature> GetLivingCreaturesStable(ICombatState? combatState)
    {
        if (combatState == null)
            return [];

        return combatState.Creatures
            .Where(creature => creature.IsAlive)
            .OrderBy(creature => creature.CombatId ?? uint.MaxValue)
            .ThenBy(creature => creature.ModelId.ToString())
            .ThenBy(creature => creature.SlotName ?? string.Empty)
            .ToList();
    }
}
