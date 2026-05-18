using ReAstralPartyMod.ReAstralPartyCardCode.Online;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillAnomalyMaker : AstralPartyCardModel
{
    private const int CardsToShow = 3;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [ReAstralPartyMod.ReAstralPartyCardCode.Keywords.AstralKeywords.AstralUnique];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    public SkillAnomalyMaker() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (CombatState == null || Owner == null)
            return;

        var offeredCards = AstralAnomalyEventCardPool.CreateStableAnomalyMakerCardsForPlayer(Owner, this, CardsToShow);
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

        await TroubleMakerTransformPreviewHelper.PlayTransformPreviewAsync(Owner, this, selectedCard);

        var cardToPlay = CombatState.CreateCard(selectedCard.CanonicalInstance ?? selectedCard, Owner);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
    }
}

