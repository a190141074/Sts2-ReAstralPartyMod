using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 208)]
public class EventAnomalyLoveCatTv : AstralPartyCardModel
{
    private const decimal TemporaryStrengthLoss = 1m;
    private const decimal TemporaryDexterityLoss = 1m;
    private const decimal EnemyStrengthLoss = 2m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<AstralTemporaryStrengthPower>(),
        HoverTipFactory.FromPower<AstralTemporaryDexterityPower>(),
        HoverTipFactory.FromPower<StrengthPower>()
    ];

    public EventAnomalyLoveCatTv() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null)
            return;

        foreach (var player in CombatState.Players)
        {
            await AstralTemporaryStrengthPower.Apply(
                player.Creature,
                -TemporaryStrengthLoss,
                this,
                Owner.Creature,
                this,
                true);
            await AstralTemporaryDexterityPower.Apply(
                player.Creature,
                -TemporaryDexterityLoss,
                this,
                Owner.Creature,
                this,
                true);
        }

        foreach (var enemy in CombatState.Creatures.Where(creature =>
                     creature.IsAlive && creature.Side != Owner.Creature.Side))
            await PowerCmd.Apply<StrengthPower>(enemy, -EnemyStrengthLoss, Owner.Creature, this, false);
    }
}
