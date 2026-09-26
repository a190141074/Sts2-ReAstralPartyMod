using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using STS2RitsuLib.Combat.CardTargeting;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public sealed class SkillSleightOfHand : PersonSkillCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<WindfallHitsPower>()
    ];

    public SkillSleightOfHand() : base(0, CardType.Skill, CardRarity.Rare, CandyPotionTargetingHelper.AnyOtherLivingPlayerOrEnemy)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var owner = Owner;
        var target = cardPlay.Target;
        if (owner?.Creature == null || target == null || !CandyPotionTargetingHelper.IsOtherLivingPlayerOrEnemy(target, owner))
            return;

        var targetStarLight = target.GetPower<StarLightPower>();
        if (targetStarLight != null && targetStarLight.Amount > 0m)
            await PowerCmd.ModifyAmount(targetStarLight, -Math.Min(4m, targetStarLight.Amount), owner.Creature, this, true);

        await PowerCmd.Apply<WindfallHitsPower>(owner.Creature, 4m, owner.Creature, this, false);

        if (target.Player == null)
            return;

        var hand = PileType.Hand.GetPile(target.Player).Cards;
        var legalCards = hand.Where(card => ModelDb.Enchantment<WindfallHitsEnchantment>().CanEnchant(card)).ToList();
        if (legalCards.Count == 0)
            return;

        var selected = DeterministicMultiplayerChoiceHelper.PickDeterministically(
            legalCards,
            card => $"{GetHandIndex(hand, card):D4}|{(card.CanonicalInstance?.Id ?? card.Id)}|{card.CurrentUpgradeLevel}",
            CardId,
            target.Player.NetId,
            hand.Count,
            GetStableCardInstanceKey(cardPlay.Card),
            GetCombatPlaySequence(cardPlay));
        if (selected == null)
            return;
        CardCmd.Enchant<WindfallHitsEnchantment>(selected, 1m);
        MainFile.Logger.Info($"[LegendaryThief] SleightOfHand synced: skill={GetStableCardInstanceKey(cardPlay.Card)} target={target.Player.NetId} handCard={GetStableCardInstanceKey(selected)} play={GetCombatPlaySequence(cardPlay)}");
    }

    private static int GetHandIndex(IReadOnlyList<CardModel> hand, CardModel card)
    {
        for (var i = 0; i < hand.Count; i++)
            if (ReferenceEquals(hand[i], card))
                return i;
        return -1;
    }

    private static string GetStableCardInstanceKey(CardModel card) =>
        $"{card.CanonicalInstance?.Id ?? card.Id}:{card.CurrentUpgradeLevel}";

    private static int GetCombatPlaySequence(CardPlay cardPlay) => cardPlay.PlayIndex;
}
