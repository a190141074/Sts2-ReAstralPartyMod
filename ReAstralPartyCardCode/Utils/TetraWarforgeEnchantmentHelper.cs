using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class TetraWarforgeEnchantmentHelper
{
    private const decimal PenaltyAmount = -1m;
    private const decimal BonusAmount = 4m;
    private const string AttackExtraCardTextKey =
        "RE_ASTRAL_PARTY_MOD_ENCHANTMENT_TETRA_WARFORGE_ENCHANTMENT.extraCardText_attack";
    private const string SkillExtraCardTextKey =
        "RE_ASTRAL_PARTY_MOD_ENCHANTMENT_TETRA_WARFORGE_ENCHANTMENT.extraCardText_skill";
    private const string CanonicalExtraCardTextKey =
        "RE_ASTRAL_PARTY_MOD_ENCHANTMENT_TETRA_WARFORGE_ENCHANTMENT.extraCardText";

    public static bool HasWarforge(CardModel? card)
    {
        return card?.Enchantment is TetraWarforgeEnchantment;
    }

    public static bool ShouldForceCombatHooks(CardModel? card)
    {
        return HasWarforge(card);
    }

    public static decimal GetDamagePenalty(CardModel card, CardModel? cardSource)
    {
        if (cardSource != card)
            return 0m;
        if (!HasWarforge(card))
            return 0m;
        if (card.Type != CardType.Attack)
            return 0m;

        return PenaltyAmount;
    }

    public static decimal GetBlockPenalty(CardModel card, CardModel? cardSource)
    {
        if (cardSource != card)
            return 0m;
        if (!HasWarforge(card))
            return 0m;
        if (card.Type != CardType.Skill)
            return 0m;

        return PenaltyAmount;
    }

    public static async Task HandleAfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var owner = card?.Owner;
        var ownerCreature = owner?.Creature;
        if (card == null || owner == null || ownerCreature == null)
            return;
        if (!HasWarforge(card))
            return;

        if (card.Type == CardType.Attack)
        {
            await CreatureCmdCompat.GainBlock(ownerCreature, BonusAmount, ValueProp.Move, null);
            return;
        }

        if (card.Type != CardType.Skill)
            return;

        var combatState = ownerCreature.CombatState;
        if (combatState == null)
            return;

        var target = owner.RunState.Rng.CombatTargets.NextItem(
            combatState.GetOpponentsOf(ownerCreature).Where(static creature => creature.IsAlive));
        if (target == null)
            return;

        await CreatureCmdCompat.Damage(
            choiceContext,
            target,
            BonusAmount,
            ValueProp.Move,
            ownerCreature,
            card);
    }

    public static LocString? ResolveDynamicExtraCardText(EnchantmentModel enchantment)
    {
        if (enchantment is not TetraWarforgeEnchantment warforge)
            return null;
        if (!warforge.HasExtraCardText || warforge.Status == EnchantmentStatus.Disabled)
            return null;
        if (warforge.IsCanonical || warforge.Card == null)
            return new LocString("enchantments", CanonicalExtraCardTextKey);

        return warforge.Card.Type switch
        {
            CardType.Attack => new LocString("enchantments", AttackExtraCardTextKey),
            CardType.Skill => new LocString("enchantments", SkillExtraCardTextKey),
            _ => new LocString("enchantments", CanonicalExtraCardTextKey)
        };
    }
}
