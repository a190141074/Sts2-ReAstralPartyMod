using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Enchantments;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillHealingSlimeDoppelganger : AstralPartyCardModel
{
    private const decimal HealAmount = 3m;
    private const decimal EnchantAmount = 1m;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override string PortraitBasePath => "res://ReAstralPartyMod/images/card_portraits/skill_healing_slime";

    protected override string FrameBasePath => PortraitBasePath;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<EffectsHealPower>()
    ];

    public SkillHealingSlimeDoppelganger() : base(
        0,
        CardType.Skill,
        CardRarity.Ancient,
        TargetType.AnyPlayer)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        var ownerCreature = Owner?.Creature;
        var target = cardPlay.Target ?? ownerCreature;
        var targetPlayer = target?.Player;
        if (ownerCreature == null || target == null || targetPlayer == null)
            return;

        await PowerCmd.Apply<EffectsHealPower>(
            target,
            HealAmount,
            ownerCreature,
            this,
            false
        );

        var selectedCard = SelectEnchantTarget(targetPlayer, cardPlay);
        if (selectedCard == null)
            return;

        CardCmd.Enchant<MiniLuluEnchantment>(selectedCard, EnchantAmount);
    }

    private CardModel? SelectEnchantTarget(MegaCrit.Sts2.Core.Entities.Players.Player targetPlayer, CardPlay cardPlay)
    {
        var handCards = PileType.Hand.GetPile(targetPlayer).Cards;
        var legalCards = handCards
            .Where(IsEligibleEnchantTarget)
            .ToList();
        if (legalCards.Count == 0)
            return null;

        return DeterministicMultiplayerChoiceHelper.PickDeterministically(
            legalCards,
            card => BuildStableHandCardKey(targetPlayer, card),
            CardId,
            targetPlayer.NetId,
            handCards.Count,
            cardPlay.Card.Id.Entry);
    }

    private static bool IsEligibleEnchantTarget(CardModel card)
    {
        return card.Enchantment == null
               && !card.EnergyCost.CostsX
               && card.EnergyCost.GetResolved() >= 1m
               && ModelDb.Enchantment<MiniLuluEnchantment>().CanEnchant(card);
    }

    private static string BuildStableHandCardKey(MegaCrit.Sts2.Core.Entities.Players.Player owner, CardModel card)
    {
        var handCards = PileType.Hand.GetPile(owner).Cards;
        var index = -1;
        for (var i = 0; i < handCards.Count; i++)
        {
            if (!ReferenceEquals(handCards[i], card))
                continue;

            index = i;
            break;
        }

        return $"{index:D4}|{(card.CanonicalInstance?.Id ?? card.Id).Entry}|{card.CurrentUpgradeLevel}";
    }
}
