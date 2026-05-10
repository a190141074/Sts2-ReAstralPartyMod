using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyRichCat : AstralPartyCardModel
{
    private const decimal HalfLifeHealAmount = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<HalfLifeHealPower>(),
        HoverTipFactory.FromPower<AnomalyFreeNextAttackPower>()
    ];

    public EventAnomalyRichCat() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
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
        {
            await PowerCmd.Apply<HalfLifeHealPower>(player.Creature, HalfLifeHealAmount, Owner?.Creature, this, false);
            var freeAttack = ModelDb.Power<AnomalyFreeNextAttackPower>().ToMutable();
            await PowerCmd.Apply(freeAttack, player.Creature, 1m, Owner?.Creature, this, false);
        }
    }
}
