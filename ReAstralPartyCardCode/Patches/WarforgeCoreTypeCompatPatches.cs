using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Patching.Models;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Patches;

public sealed class AttackPotionWarforgeCompatPatch : IPatchMethod
{
    public static string PatchId => "attack_potion_warforge_compat_patch";
    public static string Description => "Gameplay patch: let Attack Potion include Warforge-enhanted Skill cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(AttackPotion), "OnUse", [typeof(PlayerChoiceContext), typeof(Creature)])];
    }

    public static bool Prefix(AttackPotion __instance, PlayerChoiceContext choiceContext, Creature? target, ref Task __result)
    {
        __result = UseAttackPotionAsync(__instance, choiceContext);
        return false;
    }

    private static async Task UseAttackPotionAsync(AttackPotion potion, PlayerChoiceContext choiceContext)
    {
        var cards = CardFactory.GetDistinctForCombat(
                potion.Owner,
                potion.Owner.Character.CardPool
                    .GetUnlockedCards(potion.Owner.UnlockState, potion.Owner.RunState.CardMultiplayerConstraint)
                    .Where(WarforgeEnchantmentHelper.CountsAsAttack),
                3,
                potion.Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        var selected = await MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            cards,
            potion.Owner,
            canSkip: true);
        if (selected == null)
            return;

        selected.SetToFreeThisTurn();
        await MegaCrit.Sts2.Core.Commands.CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, potion.Owner, CardPilePosition.Top);
    }
}

public sealed class SkillPotionWarforgeCompatPatch : IPatchMethod
{
    public static string PatchId => "skill_potion_warforge_compat_patch";
    public static string Description => "Gameplay patch: let Skill Potion include Warforge-enhanted Attack cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(SkillPotion), "OnUse", [typeof(PlayerChoiceContext), typeof(Creature)])];
    }

    public static bool Prefix(SkillPotion __instance, PlayerChoiceContext choiceContext, Creature? target, ref Task __result)
    {
        __result = UseSkillPotionAsync(__instance, choiceContext);
        return false;
    }

    private static async Task UseSkillPotionAsync(SkillPotion potion, PlayerChoiceContext choiceContext)
    {
        var cards = CardFactory.GetDistinctForCombat(
                potion.Owner,
                potion.Owner.Character.CardPool
                    .GetUnlockedCards(potion.Owner.UnlockState, potion.Owner.RunState.CardMultiplayerConstraint)
                    .Where(WarforgeEnchantmentHelper.CountsAsSkill),
                3,
                potion.Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        var selected = await MegaCrit.Sts2.Core.Commands.CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            cards,
            potion.Owner,
            canSkip: true);
        if (selected == null)
            return;

        selected.SetToFreeThisTurn();
        await MegaCrit.Sts2.Core.Commands.CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, potion.Owner, CardPilePosition.Top);
    }
}

public sealed class FreeAttackPowerWarforgeBeforePlayedCompatPatch : IPatchMethod
{
    public static string PatchId => "free_attack_power_warforge_before_played_compat_patch";
    public static string Description => "Gameplay patch: let Free Attack Power consume on Warforge-enhanced Skill cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(FreeAttackPower), nameof(FreeAttackPower.BeforeCardPlayed), [typeof(CardPlay)])
        ];
    }

    public static void Postfix(FreeAttackPower __instance, CardPlay cardPlay, ref Task __result)
    {
        __result = ContinueBeforeCardPlayed(__result, __instance, cardPlay);
    }

    private static async Task ContinueBeforeCardPlayed(Task originalTask, FreeAttackPower power, CardPlay cardPlay)
    {
        await originalTask;
        if (power.Owner == null)
            return;
        if (cardPlay.Card.Owner?.Creature != power.Owner)
            return;
        if (!WarforgeEnchantmentHelper.CountsAsAttack(cardPlay.Card))
            return;
        if (cardPlay.Card.Pile?.Type is not (PileType.Hand or PileType.Play))
            return;

        await MegaCrit.Sts2.Core.Commands.PowerCmd.Decrement(power);
    }
}

public sealed class FreeSkillPowerWarforgeBeforePlayedCompatPatch : IPatchMethod
{
    public static string PatchId => "free_skill_power_warforge_before_played_compat_patch";
    public static string Description => "Gameplay patch: let Free Skill Power consume on Warforge-enhanced Attack cards";
    public static bool IsCritical => false;

    public static ModPatchTarget[] GetTargets()
    {
        return
        [
            new(typeof(FreeSkillPower), nameof(FreeSkillPower.BeforeCardPlayed), [typeof(CardPlay)])
        ];
    }

    public static void Postfix(FreeSkillPower __instance, CardPlay cardPlay, ref Task __result)
    {
        __result = ContinueBeforeCardPlayed(__result, __instance, cardPlay);
    }

    private static async Task ContinueBeforeCardPlayed(Task originalTask, FreeSkillPower power, CardPlay cardPlay)
    {
        await originalTask;
        if (power.Owner == null)
            return;
        if (cardPlay.Card.Owner?.Creature != power.Owner)
            return;
        if (!WarforgeEnchantmentHelper.CountsAsSkill(cardPlay.Card))
            return;
        if (cardPlay.Card.Pile?.Type is not (PileType.Hand or PileType.Play))
            return;

        await MegaCrit.Sts2.Core.Commands.PowerCmd.Decrement(power);
    }
}
