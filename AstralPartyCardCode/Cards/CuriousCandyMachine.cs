using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using AstralPartyMod.AstralPartyCardCode.Relics;
using AstralPartyMod.AstralPartyCardCode.Utils;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(EventCardPool))]
public class CuriousCandyMachine : AstralPartyCardModel
{
    private const int GoldCost = 12;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain, CardKeyword.Exhaust];

    protected override bool IsPlayable => Owner != null && Owner.Gold >= GoldCost;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ModificationPower>(),
        HoverTipFactory.FromPower<DoomPower>()
    ];

    public CuriousCandyMachine() : base(
        0,
        CardType.Skill,
        CardRarity.Rare,
        TargetType.Self,
        false)
    {
    }

    protected override void OnUpgrade()
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null || Owner.Gold < GoldCost)
            return;

        await PlayerCmd.LoseGold(GoldCost, Owner, GoldLossType.Spent);
        Owner.GetRelic<TokenGoldStarCoinHammer>()?.RefreshDisplayedBonusDamage();
        CandyMachineHelper.GrantRandomCandyPotion(Owner);
    }
}