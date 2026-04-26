using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveInfiniteSnake : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDragonPalaceSeries)
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1m : amount;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        var handPile = PileType.Hand.GetPile(Owner);
        var cardsToDraw = 8 - handPile.Cards.Count;
        if (cardsToDraw <= 0)
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, cardsToDraw, Owner);
    }
}