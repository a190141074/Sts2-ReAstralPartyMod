using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.ValueProps;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 209)]
public class EventAnomalyBrokenLeg : AstralPartyCardModel
{
    private const decimal DamageAmount = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<AnomalyGiantRockNextTurnPower>()
    ];

    public EventAnomalyBrokenLeg() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null)
            return;

        foreach (var creature in EventCombatTargetHelper.GetAliveCreaturesExcludingPlayerSummons(CombatState))
            await CreatureCmd.Damage(choiceContext, creature, DamageAmount, ValueProp.Unpowered, Owner?.Creature, this);

        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(CombatState))
        {
            var giantRockNextTurn = ModelDb.Power<AnomalyGiantRockNextTurnPower>().ToMutable();
            await PowerCmd.Apply(giantRockNextTurn, player.Creature, 1m, Owner?.Creature, this, false);
        }
    }
}
