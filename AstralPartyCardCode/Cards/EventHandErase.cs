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

namespace AstralPartyMod.AstralPartyCardCode.cards;

// 6. 手牌抹除：给所有友方单位弃掉从右往左第一张牌
[Pool(typeof(EventCardPool))]
public class EventHandErase : AstralPartyCardModel
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public EventHandErase() : base(
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
            var hand = PileType.Hand.GetPile(player);
            if (hand.Cards.Count > 0)
            {
                // 从右往左第一张牌，即最后添加到手牌的牌
                var cardToDiscard = hand.Cards.LastOrDefault();
                if (cardToDiscard != null)
                    await CardPileCmd.Add(cardToDiscard, PileType.Discard.GetPile(player)); // 添加到弃牌堆
            }
        }
    }
}