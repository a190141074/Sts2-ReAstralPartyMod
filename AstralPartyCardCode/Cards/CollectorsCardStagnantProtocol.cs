using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace AstralPartyMod.AstralPartyCardCode.cards;

[Pool(typeof(ColorlessCardPool))]
public class CollectorsCardStagnantProtocol : AstralPartyCardModel
{
    private const decimal StagnantCap = 100m;

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<StagnantCosmosPower>(),
        HoverTipFactory.FromPower<CosmosFreezesPower>()
    ];

    public CollectorsCardStagnantProtocol() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner?.Creature == null)
            return;

        await PowerCmd.Apply<StagnantCosmosPower>(
            Owner.Creature,
            StagnantCap,
            Owner.Creature,
            this,
            false
        );
    }
}
