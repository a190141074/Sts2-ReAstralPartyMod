using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using System.Linq;
using System.Threading.Tasks;

namespace AstralPartyMod.AstralPartyCardCode.cards;

/*
 * 战斗，爽！：给所有友方单位发一张原版游戏卡牌：巨石(Giant Rock)（消耗+虚无）
 */
[Pool(typeof(EventCardPool))]
public class EventFightFun : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventFightFun() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.AllAllies)
    {
    }

    protected override void OnUpgrade()
    {
        // 升级时不添加额外效果
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (CombatState == null) return;

        var giantRockCard = ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.GiantRock>();

        foreach (var player in CombatState.Players)
        {
            var card = CombatState.CreateCard(giantRockCard, player);
            CardCmd.Upgrade(card); // 将卡牌升级
            card.AddKeyword(CardKeyword.Exhaust);
            card.AddKeyword(CardKeyword.Ethereal);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true);
        }
    }
}