using System.Collections.Generic;
using AstralPartyMod.AstralPartyCardCode.Keywords;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.ValueProps;

namespace AstralPartyMod.AstralPartyCardCode.Relics;

[Pool(typeof(SharedRelicPool))]
public class TokenExclusiveAncientWand : AstralPartyRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(AstralKeywords.AstralMagicAcademySeries)
    ];

    public override decimal ModifyMaxEnergy(Player player, decimal amount)
    {
        return player == Owner ? amount + 1m : amount;
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner?.Creature == null || dealer != Owner.Creature)
            return 0m;
        if (target == null || target.Side == Owner.Creature.Side)
            return 0m;
        if (amount <= 0m)
            return 0m;
        if (cardSource?.Owner != Owner || cardSource.Type != CardType.Skill)
            return 0m;

        return 3m;
    }
}