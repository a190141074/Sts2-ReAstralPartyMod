using System.Linq;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(ColorlessCardPool))]
public class SkillTroubleMaker : AstralPartyCardModel
{
    private static readonly LocString SelectionPrompt = new("cards", "SKILL_TROUBLE_MAKER.select_prompt");
    private static int _choosePromptOverrideDepth;

    private int _cardsToShow = 2;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override IEnumerable<DynamicVar> CanonicalVars => [new IntVar("StarLight", 3)];

    public SkillTroubleMaker() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        _cardsToShow += 1;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner == null) return;

        var offeredCards = ModelDb.AllCards
            .Where(card => card is AstralPartyCardModel)
            .Where(card => card.GetType().Name.StartsWith("Event"))
            .Where(card => card.GetType() != typeof(SkillTroubleMaker))
            .OrderBy(_ => Owner.RunState.Rng.Niche.NextInt(int.MaxValue))
            .Select(card =>
            {
                var mutableCard = card.ToMutable();
                mutableCard.Owner = Owner;
                return mutableCard;
            })
            .Take(_cardsToShow)
            .ToList();

        if (offeredCards.Count == 0) return;

        BeginChoosePromptOverride();
        CardModel? selectedCard;
        try
        {
            selectedCard = await CardSelectCmd.FromChooseACardScreen(choiceContext, offeredCards, Owner, false);
        }
        finally
        {
            EndChoosePromptOverride();
        }

        if (selectedCard == null) return;

        await PowerCmd.Apply(ModelDb.Power<StarLightPower>().ToMutable(), Owner.Creature,
            DynamicVars["StarLight"].BaseValue, Owner.Creature, this, false);

        var cardToPlay = CombatState.CreateCard(selectedCard.CanonicalInstance, Owner);
        await CardCmd.AutoPlay(choiceContext, cardToPlay, Owner.Creature, AutoPlayType.Default, false, true);
    }

    public static bool ShouldOverrideChoosePrompt => _choosePromptOverrideDepth > 0;

    public static string GetChoosePromptText()
    {
        return LocManager.Instance.SmartFormat(SelectionPrompt, []);
    }

    private static void BeginChoosePromptOverride()
    {
        _choosePromptOverrideDepth++;
    }

    private static void EndChoosePromptOverride()
    {
        _choosePromptOverrideDepth = Math.Max(0, _choosePromptOverrideDepth - 1);
    }
}