using ReAstralPartyMod.ReAstralPartyCardCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

[RegisterCard(typeof(EventCardPool))]
public class EventAnomalyDemonLordProtection : AstralPartyCardModel
{
    private const decimal MarkAmount = 3m;
    private const decimal VulnerableAmount = 2m;
    private const decimal WeakAmount = 2m;
    private const decimal StarLightAmount = 5m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<MarkLockPower>(),
        HoverTipFactory.FromPower<VulnerablePower>(),
        HoverTipFactory.FromPower<WeakPower>(),
        HoverTipFactory.FromPower<StarLightPower>()
    ];

    public EventAnomalyDemonLordProtection() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null || Owner?.Creature == null)
            return;

        foreach (var enemy in CombatState.Creatures.Where(creature =>
                     creature.IsAlive && creature.Side != Owner.Creature.Side))
        {
            await PowerCmd.Apply<MarkLockPower>(enemy, MarkAmount, Owner.Creature, this, false);
            await PowerCmd.Apply<VulnerablePower>(enemy, VulnerableAmount, Owner.Creature, this, false);
            await PowerCmd.Apply<WeakPower>(enemy, WeakAmount, Owner.Creature, this, false);
        }

        foreach (var player in CombatState.Players)
            await PowerCmd.Apply<StarLightPower>(player.Creature, StarLightAmount, Owner.Creature, this, false);
    }
}
