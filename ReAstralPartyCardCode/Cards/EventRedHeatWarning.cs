using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using ReAstralPartyMod.ReAstralPartyCardCode.Utils;

namespace ReAstralPartyMod.ReAstralPartyCardCode.cards;

/*
 * 红温警告
 * 所有玩家获�?活力
 */
[RegisterCard(typeof(EventCardPool))]
[RegisterCard(typeof(AstralEventCardPool), Order = 10)]
public class EventRedHeatWarning : AstralPartyCardModel
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new PowerVar<VigorPower>(5m)];

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [HoverTipFactory.FromPower<VigorPower>()];


    public EventRedHeatWarning() : base(0, CardType.Skill, CardRarity.Rare, TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        foreach (var player in EventCombatTargetHelper.GetAlivePlayers(CombatState))
            await PowerCmd.Apply<VigorPower>(player.Creature, DynamicVars["VigorPower"].BaseValue, Owner.Creature,
                this);
    }
}
