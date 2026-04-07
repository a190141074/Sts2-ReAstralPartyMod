using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;
/*
 * 蒸蛋事件卡
 * 对全场单位恢复生命值
 */

// 加入哪个卡池
[Pool(typeof(ColorlessCardPool))]
public class EventAngelsDescent : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new HealVar(6m)];

    public EventAngelsDescent() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
        // WithVar("Heal", 6);
        // WithKeywords(CardKeyword.Exhaust);
        // WithKeywords(AstralKeywords.EventCard);
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        foreach (var creature in CombatState.Creatures) await CreatureCmd.Heal(creature, DynamicVars["Heal"].BaseValue);
    }
}