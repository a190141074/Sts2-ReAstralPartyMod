using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Utils;

internal static class MiniLuluEnchantmentHelper
{
    private const decimal BaseHealAmount = 1m;
    private const decimal ExtraHealCost = 1m;

    public static bool HasMiniLulu(CardModel? card)
    {
        return card?.Enchantment is MiniLuluEnchantment;
    }

    public static bool ShouldForceCombatHooks(CardModel? card)
    {
        return HasMiniLulu(card);
    }

    public static async Task HandleAfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var owner = card?.Owner;
        var ownerCreature = owner?.Creature;
        if (card == null || owner == null || ownerCreature == null)
            return;
        if (!HasMiniLulu(card))
            return;

        await CreatureCmd.Heal(ownerCreature, BaseHealAmount, true);

        if (!HasEligibleSlimePersona(owner))
            return;

        var halfLifeHeal = ownerCreature.GetPower<EffectsHealPower>();
        if (halfLifeHeal == null || halfLifeHeal.Amount < ExtraHealCost)
            return;

        await PowerCmd.ModifyAmount(halfLifeHeal, -ExtraHealCost, ownerCreature, card, true);
        await CreatureCmd.Heal(ownerCreature, BaseHealAmount, true);
    }

    private static bool HasEligibleSlimePersona(MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        return owner.GetRelic<PersonSlimeLulu>() != null
               || owner.GetRelic<PersonSlimeLuluDoppelganger>() != null;
    }
}
