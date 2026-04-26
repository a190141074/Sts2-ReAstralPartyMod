using System.Collections.Generic;
using System.Threading.Tasks;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using AstralPartyMod.AstralPartyCardCode.Powers;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveLittleSnakeDoll : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    public override bool ShouldReceiveCombatHooks => true;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromPower<ReversedScalesPower>(),
        HoverTipFactory.FromPower<CounterPower>(),
        HoverTipFactory.FromKeyword(AstralKeywords.AstralDragonPalaceSeries)
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || target != Owner.Creature)
            return;
        if (result.TotalDamage <= 0m)
            return;

        Flash();
        await PowerCmd.Apply<ReversedScalesPower>(Owner.Creature, 1m, Owner.Creature, cardSource, false);
    }
}