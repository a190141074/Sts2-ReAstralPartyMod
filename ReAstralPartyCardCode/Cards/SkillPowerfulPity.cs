using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Events;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class SkillPowerfulPity : AstralPartyCardModel
{
    private const int MaxSelectedCards = 3;

    private static readonly LocString SelectionPrompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_POWERFUL_PITY.select_prompt");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StarLightPower>(),
        TokenEternalStarlight.BuildReferenceHoverTip()
    ];

    public SkillPowerfulPity() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AnyPlayer)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target ?? ownerCreature;
        var targetPlayer = target?.Player;
        if (owner == null || ownerCreature == null || targetPlayer == null)
            return;

        var handCards = PileType.Hand.GetPile(owner).Cards.ToList();
        var selectedCards = await SelectCards(choiceContext, handCards);

        foreach (var selectedCard in selectedCards)
        {
            var copiedCard = CreateCopiedCardForTarget(ownerCreature, selectedCard, targetPlayer);
            if (!copiedCard.Keywords.Contains(CardKeyword.Exhaust))
            {
                CardCmd.ApplyKeyword(copiedCard, CardKeyword.Exhaust);
            }

            await PersonaMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(
                copiedCard,
                true,
                CardPilePosition.Top,
                this);
        }

        if (selectedCards.Count > 0)
        {
            await XiaoLeiAwakeningHelper.TryGrantAwakeningForGrantedCard(owner, targetPlayer, selectedCards.Count);

            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                ownerCreature,
                selectedCards.Count,
                ownerCreature,
                this,
                false
            );

            await CardCmd.Discard(choiceContext, selectedCards);
        }

        await TokenEternalStarlight.GrantStacks(owner, 1);
    }

    private static CardModel CreateCopiedCardForTarget(
        Creature ownerCreature,
        CardModel selectedCard,
        MegaCrit.Sts2.Core.Entities.Players.Player targetPlayer)
    {
        var combatState = targetPlayer.Creature?.CombatState ?? ownerCreature.CombatState;
        if (combatState == null)
            throw new InvalidOperationException("Cannot copy a combat card for a player without an active combat state.");

        var copiedCard = combatState.CreateCard(selectedCard.CanonicalInstance, targetPlayer);
        CopySupportedCombatState(selectedCard, copiedCard);
        return copiedCard;
    }

    private static void CopySupportedCombatState(CardModel sourceCard, CardModel copiedCard)
    {
        CopyUpgradeLevel(sourceCard, copiedCard);

        if (sourceCard is MadScience sourceMadScience && copiedCard is MadScience copiedMadScience)
        {
            copiedMadScience.TinkerTimeType = sourceMadScience.TinkerTimeType;
            copiedMadScience.TinkerTimeRider = sourceMadScience.TinkerTimeRider;
        }
    }

    private static void CopyUpgradeLevel(CardModel sourceCard, CardModel copiedCard)
    {
        while (copiedCard.CurrentUpgradeLevel < sourceCard.CurrentUpgradeLevel)
        {
            copiedCard.UpgradeInternal();
            copiedCard.FinalizeUpgradeInternal();
        }
    }

    private async Task<List<CardModel>> SelectCards(PlayerChoiceContext choiceContext, List<CardModel> handCards)
    {
        if (Owner == null || handCards.Count == 0)
            return [];

        var prefs = new CardSelectorPrefs(SelectionPrompt, Math.Min(MaxSelectedCards, handCards.Count))
        {
            Cancelable = true
        };

        return (await CardSelectCmd.FromSimpleGrid(choiceContext, handCards, Owner, prefs)).ToList();
    }
}
