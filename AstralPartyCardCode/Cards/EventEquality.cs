using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.cards;

// 8. 人人平等：所有人的生命降低为一点，并获得50点护盾
[Pool(typeof(ColorlessCardPool))]
public class EventEquality : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(50m, ValueProp.Move)];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventEquality() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        foreach (var player in CombatState.Players)
        {
            // 设置生命值为1点
            await CreatureCmd.SetCurrentHp(player.Creature, 1);
            // 获得护盾
            await CreatureCmd.GainBlock(player.Creature, DynamicVars["Block"].BaseValue, ValueProp.Move, null);
        }
    }
}