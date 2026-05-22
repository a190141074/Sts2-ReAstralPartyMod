using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class EyeOfSunEnchantmentHelper
{
    private const int PlaysPerTrigger = 10;
    private const int BaseBurnAmount = 9;
    private const int BurnIncreasePerTrigger = 3;

    public static bool HasEyeOfSun(CardModel? card)
    {
        return card?.Enchantment is EssenceEyeOfSunEnchantment;
    }

    public static bool ShouldForceCombatHooks(CardModel? card)
    {
        return HasEyeOfSun(card);
    }

    public static async Task HandleAfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card?.Owner?.Creature == null)
            return;
        if (card.Enchantment is not EssenceEyeOfSunEnchantment enchantment)
            return;

        enchantment.AstralParty_EyeOfSunPlayedCount++;
        if (enchantment.AstralParty_EyeOfSunPlayedCount % PlaysPerTrigger != 0)
            return;

        var burnAmount = BaseBurnAmount + enchantment.AstralParty_EyeOfSunTriggerCount * BurnIncreasePerTrigger;
        enchantment.AstralParty_EyeOfSunTriggerCount++;

        var combatState = card.Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var enemies = combatState
            .GetOpponentsOf(card.Owner.Creature)
            .Where(static creature => creature.IsAlive)
            .ToList();
        if (enemies.Count == 0)
            return;

        foreach (var enemy in enemies)
            await PowerCmd.Apply<BlazingSolarBurnPower>(enemy, burnAmount, card.Owner.Creature, card, false);
    }
}
