using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models;
using ReAstralPartyMod.ReAstralPartyCardCode.Compat.Windchaser;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool))]
public class SkillGrantSpark : AstralPartyCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique, CardKeyword.Retain, CardKeyword.Exhaust];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public SkillGrantSpark() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.Self, WindchaserCompat.IsLoaded())
    {
    }

    protected override void OnUpgrade()
    {
    }

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card != this)
            return false;
        if (!IsUpgraded)
            return false;
        if (card.EnergyCost.CostsX)
            return false;

        modifiedCost = 0m;
        return originalCost != modifiedCost;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner?.Creature?.CombatState == null)
            return;

        var offeredCards = WindchaserSpellbookHelper.CreateUpgradedSpellbookCardsForPlayer(Owner);
        if (offeredCards.Count == 0)
            return;

        var selectedCard = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
            choiceContext,
            Owner,
            offeredCards,
            false,
            $"{Id.Entry}.play");
        if (selectedCard == null)
            return;

        var cardToAdd = Owner.Creature.CombatState.CreateCard(selectedCard.CanonicalInstance ?? selectedCard, Owner);
        CardCmd.Upgrade(cardToAdd);
        await PersonMultiplayerEffectHelper.AddGeneratedCardToHandAndNotify(cardToAdd, true, CardPilePosition.Top, this);
    }
}
