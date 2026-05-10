using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyJustAMob : AstralPartyCardModel
{
    private const decimal TemporaryDexterityAmount = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>()
    ];

    public EventAnomalyJustAMob() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
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
            await AstralTemporaryDexterityPower.Apply(player.Creature, TemporaryDexterityAmount, this, Owner?.Creature, this, true);
    }
}
