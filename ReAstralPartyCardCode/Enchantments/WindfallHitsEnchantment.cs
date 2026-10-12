using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Scaffolding.Content;

namespace ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;

[RegisterEnchantment]
public sealed class WindfallHitsEnchantment : ModEnchantmentTemplate
{
    public override EnchantmentAssetProfile AssetProfile => new()
    {
        IconPath = "res://ReAstralPartyMod/images/enchantments/windfall_hits_enchantment.png"
    };

    public override bool CanEnchantCardType(CardType cardType)
    {
        return cardType is CardType.Attack or CardType.Skill or CardType.Power;
    }

    public override bool CanEnchant(CardModel card)
    {
        return base.CanEnchant(card) && CanEnchantCardType(card.Type) && card.Enchantment == null;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        if (Status != EnchantmentStatus.Normal || cardPlay?.Card?.Owner?.Creature == null)
            return;

        var owner = cardPlay.Card.Owner;
        var roll = DeterministicMultiplayerChoiceHelper.RollDeterministically(
            0,
            4,
            MainFile.ModId,
            "windfall_hits",
            owner.RunState.Rng.StringSeed,
            owner.NetId,
            cardPlay.PlayIndex,
            cardPlay.Card.CanonicalInstance?.Id ?? cardPlay.Card.Id);
        var amount = new[] { 1m, 2m, 3m, 5m, 10m }[Math.Clamp(roll, 0, 4)];
        await PowerCmd.Apply<EffectsStarlightPower>(
            owner.Creature,
            amount,
            owner.Creature,
            cardPlay.Card,
            false);

        // Native temporary-enchantment lifecycle: keep the instance, disable it after first use.
        Status = EnchantmentStatus.Disabled;
        MainFile.Logger.Info(
            $"[LegendaryThief] WindfallHits resolved: owner={owner.NetId} card={cardPlay.Card.CanonicalInstance?.Id ?? cardPlay.Card.Id} play={cardPlay.PlayIndex} result={amount} status={Status}");
    }
}
