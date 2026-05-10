using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyBossBurnedOut : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventAnomalyBossBurnedOut() : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature?.CombatState == null)
            return;

        var offeredCards = AstralEventCardPool.CreateStableTroubleMakerCardsForPlayer(Owner, this, 3)
            .Where(card => card is not EventDeusExMachina)
            .ToList();
        if (offeredCards.Count > 0)
        {
            var selectedCard = await DeterministicMultiplayerChoiceHelper.SelectCanonicalCardForPlayer(
                choiceContext,
                Owner,
                offeredCards,
                false,
                $"{Id.Entry}.play");

            if (selectedCard != null)
            {
                var cardToPlay = CombatState!.CreateCard(selectedCard.CanonicalInstance ?? selectedCard, Owner);
                await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
            }
        }

        foreach (var player in CombatState!.Players)
            PersonaRelicHelper.AdvanceCooldownRelics(player, 1);
    }
}
