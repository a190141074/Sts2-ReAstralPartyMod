using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(PersonSkillCardPool), StableEntryStem = "skill_token_transcend_dimensions")]
public sealed class SkillTokenTranscendDimensions : PersonSkillCardModel
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Exhaust, CardKeyword.Eternal];

    public SkillTokenTranscendDimensions() : base(
        0,
        CardType.Skill,
        CardRarity.Token,
        TargetType.Self,
        showInCardLibrary: false)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RecordTelemetryOnPlay();
        if (Owner == null)
            return;

        var cardsToRecycle = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(card => card != this)
            .ToList();

        var orderedCards = DeterministicMultiplayerChoiceHelper.OrderDeterministically(
            cardsToRecycle,
            GetStableRecycleCardKey,
            MainFile.ModId,
            Id.Entry,
            Owner.NetId,
            Owner.Creature?.CombatState?.RoundNumber ?? 0,
            "skill_token_transcend_dimensions_recycle");

        foreach (var card in orderedCards)
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Bottom, this);

        await PersonMultiplayerEffectHelper.DrawCardsForPlayer(choiceContext, 5m, Owner, this);
        await PlayerCmd.GainEnergy(5m, Owner);
    }

    private static string GetStableRecycleCardKey(CardModel card)
    {
        var pileType = card.Pile?.Type.ToString() ?? "none";
        var pileIndex = GetCardPileIndex(card);
        return $"{pileType}:{pileIndex:D4}:{card.Id.Entry}";
    }

    private static int GetCardPileIndex(CardModel card)
    {
        var pileCards = card.Pile?.Cards;
        if (pileCards == null)
            return -1;

        for (var i = 0; i < pileCards.Count; i++)
        {
            if (ReferenceEquals(pileCards[i], card))
                return i;
        }

        return -1;
    }
}
