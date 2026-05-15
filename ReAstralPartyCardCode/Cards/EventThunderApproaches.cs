using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventThunderApproaches : AstralPartyCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new PowerVar<DrawCardsNextTurnPower>(1m)
    ];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        StunIntent.GetStaticHoverTip(),
        HoverTipFactory.FromPower<RingingPower>(),
        HoverTipFactory.FromPower<DrawCardsNextTurnPower>()
    ];

    public EventThunderApproaches() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var creature in CombatState.Creatures.Where(creature =>
                     creature.IsAlive
                     && creature != Owner.Creature
                     && creature.Side != Owner.Creature.Side))
            await CreatureCmd.Stun(creature);

        foreach (var player in CombatState.Players)
        {
            await PowerCmd.Apply<RingingPower>(player.Creature, 1m, Owner.Creature, this);
            await PowerCmd.Apply<DrawCardsNextTurnPower>(player.Creature,
                DynamicVars["DrawCardsNextTurnPower"].BaseValue,
                Owner.Creature, this);
            await CardGainAttribution.RunWithSource(this,
                () => CardPileCmd.Draw(choiceContext, (int)DynamicVars["Cards"].BaseValue, player));
        }
    }
}
