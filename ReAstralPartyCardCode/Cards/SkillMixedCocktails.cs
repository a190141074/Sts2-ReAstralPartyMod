using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReAstralPartyMod.ReAstralPartyCardCode.Keywords;
using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Relics;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;


[RegisterCard(typeof(PersonaSkillCardPool))]
public class SkillMixedCocktails : AstralPartyCardModel
{
    private const int MaxDiscardCount = 3;
    private const decimal RelicDistinctTypesStarLight = 3m;
    private const decimal RelicSameTypesDexterity = 3m;

    private static readonly LocString SelectionPrompt =
        new("cards", "RE_ASTRAL_PARTY_MOD_CARD_SKILL_MIXED_COCKTAILS.select_prompt");

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [AstralKeywords.AstralUnique, AstralKeywords.AstralMixed];

    protected override bool ShouldAutoApplyCooldownEnchantment => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        AstralKeywords.CreateHoverTip(AstralKeywords.AstralMixedId),
        HoverTipFactory.FromPower<MixedCocktailsPower>(),
        HoverTipFactory.FromPower<StarLightPower>(),
        HoverTipFactory.FromPower<DexterityPower>()
    ];

    public SkillMixedCocktails() : base(
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
        RecordTelemetryOnPlay();
        var owner = Owner;
        var ownerCreature = owner?.Creature;
        var target = cardPlay.Target ?? ownerCreature;
        var targetPlayer = target?.Player;
        if (owner == null || ownerCreature == null || target == null || targetPlayer == null)
            return;

        var cardsToDiscard = await SelectCardsToDiscard(choiceContext);
        if (cardsToDiscard.Count > 0)
            await CardCmd.Discard(choiceContext, cardsToDiscard);

        MixedCocktailsPower? mixedCocktailsPower = null;
        if (cardsToDiscard.Count > 0)
            mixedCocktailsPower = await MixedCocktailsPower.GetOrCreate(target, ownerCreature, this);

        foreach (var discardedCard in cardsToDiscard)
            if (mixedCocktailsPower != null)
                await ApplyDiscardEffect(choiceContext, mixedCocktailsPower, target, targetPlayer, ownerCreature,
                    discardedCard);

        if (cardsToDiscard.Count > 0)
        {
            await CardGainAttribution.RunWithSource(this,
                () => CardPileCmd.Draw(choiceContext, cardsToDiscard.Count, owner));
            await mixedCocktailsPower!.RecordDraw(cardsToDiscard.Count);
        }

        var bartenderRelic = owner.GetRelic<PersonJillSteinle>();
        if (bartenderRelic == null || cardsToDiscard.Count != MaxDiscardCount)
            return;

        var distinctTypeCount = cardsToDiscard.Select(card => card.Type).Distinct().Count();
        if (distinctTypeCount == MaxDiscardCount)
        {
            await PowerCmd.Apply(
                ModelDb.Power<StarLightPower>().ToMutable(),
                ownerCreature,
                RelicDistinctTypesStarLight,
                ownerCreature,
                this,
                false
            );
            return;
        }

        if (distinctTypeCount != 1)
            return;

        await PowerCmd.Apply<DexterityPower>(target, RelicSameTypesDexterity, ownerCreature, this, false);
        if (mixedCocktailsPower != null)
            await mixedCocktailsPower.RecordDexterity(RelicSameTypesDexterity);
    }

    private async Task ApplyDiscardEffect(
        PlayerChoiceContext choiceContext,
        MixedCocktailsPower mixedCocktailsPower,
        Creature target,
        MegaCrit.Sts2.Core.Entities.Players.Player targetPlayer,
        Creature ownerCreature,
        CardModel discardedCard)
    {
        switch (discardedCard.Type)
        {
            case CardType.Attack:
                await mixedCocktailsPower.RecordTemporaryStrength(1m, ownerCreature, this);
                break;
            case CardType.Skill when discardedCard.GainsBlock:
                await mixedCocktailsPower.RecordTemporaryDexterity(1m, ownerCreature, this);
                break;
            case CardType.Skill:
                await CreatureCmd.Heal(target, 1m, true);
                await mixedCocktailsPower.RecordHeal(1m);
                break;
            case CardType.Power:
                await PlayerCmd.GainEnergy(1m, targetPlayer);
                await mixedCocktailsPower.RecordEnergy(1m);
                break;
            case CardType.Status:
                await CardGainAttribution.RunWithSource(this, () => CardPileCmd.Draw(choiceContext, 1m, targetPlayer));
                await mixedCocktailsPower.RecordDraw(1m);
                break;
            case CardType.Curse:
                await CreatureCmd.Heal(target, 1m, true);
                await CreatureCmd.GainBlock(target, 1m, ValueProp.Move, null);
                await mixedCocktailsPower.RecordHeal(1m);
                await mixedCocktailsPower.RecordBlock(1m);
                break;
        }
    }

    private async Task<List<CardModel>> SelectCardsToDiscard(PlayerChoiceContext choiceContext)
    {
        if (Owner == null)
            return [];

        var handCount = PileType.Hand.GetPile(Owner).Cards.Count;
        if (handCount == 0)
            return [];

        var prefs = new CardSelectorPrefs(SelectionPrompt, 0, Math.Min(MaxDiscardCount, handCount))
        {
            Cancelable = true
        };

        var selectedCards = await DeterministicMultiplayerChoiceHelper.SelectHandCardsForPlayer(
            choiceContext,
            Owner,
            prefs,
            selectionSource: null);
        return selectedCards.ToList();
    }
}

