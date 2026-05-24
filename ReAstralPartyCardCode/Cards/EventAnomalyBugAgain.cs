using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 201)]
public class EventAnomalyBugAgain : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        StunIntent.GetStaticHoverTip()
    ];

    public EventAnomalyBugAgain() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null)
            return;

        foreach (var enemy in EventCombatTargetHelper.GetAliveNonSummonEnemies(CombatState, Owner.Creature))
            await CreatureCmd.Stun(enemy);
    }
}
