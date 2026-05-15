using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyNoRetreatShrimp : AstralPartyCardModel
{
    private const decimal TemporaryStrengthAmount = 3m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<AstralTemporaryStrengthPower>()
    ];

    public EventAnomalyNoRetreatShrimp() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var player in CombatState.Players)
            await AstralTemporaryStrengthPower.Apply(player.Creature, TemporaryStrengthAmount, this, Owner?.Creature,
                this, true);
    }
}
